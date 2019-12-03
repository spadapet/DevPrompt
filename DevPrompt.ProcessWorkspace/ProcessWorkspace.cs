using DevPrompt.ProcessWorkspace.Settings;
using DevPrompt.ProcessWorkspace.UI;
using DevPrompt.ProcessWorkspace.UI.ViewModels;
using DevPrompt.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace
{
    internal sealed class ProcessWorkspace : Api.Utility.PropertyNotifier, Api.IProcessWorkspace
    {
        public Api.IWindow Window { get; }
        public Guid Id => Api.Constants.ProcessWorkspaceId;
        public string Name => ProcessWorkspace.StaticName;
        public string Tooltip => ProcessWorkspace.StaticTooltip;
        public string Title => this.activeTab?.Title ?? this.Name;
        public Api.IWorkspaceSnapshot Snapshot => new ProcessWorkspaceSnapshot(this);
        public IEnumerable<Api.ITabHolder> Tabs => this.tabs;
        public Api.IProcessHost ProcessHost => this.ViewElement.ProcessHostWindow?.ProcessHost;

        public static string StaticName => Resources.ProcessWorkspace_Name;
        public static string StaticTooltip => string.Empty;

        private readonly HashSet<ButtonInfo> tabButtons;
        private readonly ObservableCollection<TabVM> tabs;
        private readonly LinkedList<TabVM> tabOrder;
        private LinkedListNode<TabVM> currentTabCycle;
        private TabVM activeTab;
        private int newTabIndex;
        private ProcessWorkspaceControl viewElement;
        private ProcessWorkspaceSnapshot initialSnapshot;

        private const int NewTabAtEnd = -1;
        private const int NewTabAtEndNoActivate = -2;

        private sealed class ButtonInfo
        {
            public Button Button { get; }
            public ContextMenu ContextMenu => this.Button.ContextMenu;
            public TabVM Tab => this.Button.DataContext as TabVM;
            public bool ContextMenuUpdated { get; set; }

            public ButtonInfo(Button button)
            {
                this.Button = button;
            }

            public override bool Equals(object obj) => obj is ButtonInfo info && this.Button == info.Button;
            public override int GetHashCode() => this.Button.GetHashCode();
        }

        public ProcessWorkspace(Api.IWindow window, ProcessWorkspaceSnapshot snapshot = null)
        {
            this.Window = window;
            this.tabButtons = new HashSet<ButtonInfo>();
            this.tabs = new ObservableCollection<TabVM>();
            this.tabs.CollectionChanged += this.OnTabsCollectionChanged;
            this.tabOrder = new LinkedList<TabVM>();
            this.newTabIndex = ProcessWorkspace.NewTabAtEnd;
            this.initialSnapshot = snapshot;
        }

        private async Task InitTabs(ProcessWorkspaceSnapshot snapshot)
        {
            try
            {
                this.newTabIndex = ProcessWorkspace.NewTabAtEndNoActivate;

                foreach (Api.ITabSnapshot tabSnapshot in snapshot?.Tabs ?? Enumerable.Empty<Api.ITabSnapshot>())
                {
                    Api.ITabHolder tab = this.AddTab(tabSnapshot, false);

                    if (snapshot.ActiveTabIndex >= 0 &&
                        snapshot.ActiveTabIndex < snapshot.Tabs.Count &&
                        snapshot.Tabs[snapshot.ActiveTabIndex] == tabSnapshot)
                    {
                        this.ActiveTab = tab;
                    }
                }

                if (this.tabs.Count == 0)
                {
                    foreach (Api.IConsoleSettings console in this.Window.App.Settings.ConsoleSettings)
                    {
                        if (console.RunAtStartup)
                        {
                            this.RunProcess(console);
                        }
                    }
                }

                if (this.tabs.Count == 0)
                {
                    foreach (Api.IConsoleSettings console in await this.Window.App.Settings.GetVisualStudioConsoleSettingsAsync())
                    {
                        if (console.RunAtStartup)
                        {
                            this.RunProcess(console);
                        }
                    }
                }

                if (this.ActiveTab == null && this.tabs.Count > 0)
                {
                    this.ActiveTab = this.tabs[0];
                }

                this.Window.App.Telemetry.TrackEvent("ProcessWorkspace.InitTabs", new Dictionary<string, object>()
                {
                    { "TabCount", this.tabs.Count },
                    { "FromSnapshot", snapshot != null },
                });
            }
            finally
            {
                this.newTabIndex = ProcessWorkspace.NewTabAtEnd;
            }
        }

        public void AddTabButton(Button button)
        {
            ButtonInfo info = new ButtonInfo(button);
            Debug.Assert(button != null && !this.tabButtons.Contains(info));
            this.tabButtons.Add(info);
        }

        public void RemoveTabButton(Button button)
        {
            int count = this.tabButtons.RemoveWhere(i => i.Button == button);
            Debug.Assert(count == 1);
        }

        public void EnsureTab(Button button)
        {
            if (this.tabButtons.FirstOrDefault(b => b.Button == button) is ButtonInfo info && info.Button.DataContext is TabVM tab && !tab.CreatedTab)
            {
                // Force tab creation
                _ = tab.Tab;
            }
        }

        public void UpdateContextMenu(ContextMenu menu)
        {
            if (this.tabButtons.FirstOrDefault(b => b.ContextMenu == menu) is ButtonInfo info && !info.ContextMenuUpdated && info.Button.DataContext is TabVM tab)
            {
                while (menu.Items.Count > 0 && !(menu.Items[0] is Separator separator && separator.Tag is string name && name == "[Plugins]"))
                {
                    menu.Items.RemoveAt(0);
                }

                if (tab.ContextMenuItems is IEnumerable<FrameworkElement> newItems)
                {
                    int pos = 0;
                    foreach (FrameworkElement newItem in newItems)
                    {
                        menu.Items.Insert(pos++, newItem);
                    }
                }

                info.ContextMenuUpdated = true;
            }
        }

        IEnumerable<FrameworkElement> Api.IWorkspace.MenuItems => Enumerable.Empty<FrameworkElement>();
        UIElement Api.IWorkspace.ViewElement => this.ViewElement;

        IEnumerable<KeyBinding> Api.IWorkspace.KeyBindings => new KeyBinding[]
        {
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabClose), Key.F4, ModifierKeys.Control),
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabDetach), Key.F4, ModifierKeys.Control | ModifierKeys.Shift),
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabClone), Key.T, ModifierKeys.Control),
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabSetName), Key.T, ModifierKeys.Control | ModifierKeys.Shift),
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabCycleNext), Key.Tab, ModifierKeys.Control),
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabCyclePrev), Key.Tab, ModifierKeys.Control | ModifierKeys.Shift),
            new KeyBinding(new Api.Utility.DelegateCommand(this.TabContextMenu), Key.F10, ModifierKeys.Shift),
        };

        void Api.IWorkspace.OnKeyEvent(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    if (args.IsUp)
                    {
                        this.TabCycleStop();
                    }
                    break;

                case Key.Apps:
                    if (args.IsUp)
                    {
                        args.Handled = true;
                        this.TabContextMenu();
                    }
                    break;
            }
        }

        public ProcessWorkspaceControl ViewElement
        {
            get
            {
                if (this.viewElement == null)
                {
                    this.viewElement = new ProcessWorkspaceControl(this);
                    this.viewElement.Loaded += this.OnViewElementLoaded;
                }

                return this.viewElement;
            }
        }

        private async void OnViewElementLoaded(object sender, RoutedEventArgs args)
        {
            this.viewElement.Loaded -= this.OnViewElementLoaded;

            ProcessWorkspaceSnapshot snapshot = this.initialSnapshot;
            this.initialSnapshot = null;

            await this.InitTabs(snapshot);
        }

        public Api.ITabHolder ActiveTab
        {
            get => this.activeTab;

            set
            {
                TabVM oldTab = this.activeTab;
                if (this.SetPropertyValue(ref this.activeTab, value as TabVM))
                {
                    if (this.activeTab != null)
                    {
                        if (!this.tabs.Contains(this.activeTab))
                        {
                            this.AddTab(this.activeTab, activate: false);
                        }

                        this.activeTab.ActiveState = Api.ActiveState.Active;
                    }

                    if (oldTab != null)
                    {
                        oldTab.ActiveState = Api.ActiveState.Hidden;
                    }

                    this.ViewElement.ViewElement = this.activeTab?.ViewElement;

                    this.OnPropertyChanged(nameof(this.HasActiveTab));
                    this.OnPropertyChanged(nameof(this.Title));
                }

                this.FocusActiveTab();
            }
        }

        public bool HasActiveTab => this.ActiveTab != null;

        public void FocusActiveTab()
        {
            if (this.ActiveTab is TabVM tab)
            {
                if (this.currentTabCycle != null)
                {
                    if (this.currentTabCycle.Value != tab)
                    {
                        this.currentTabCycle = this.tabOrder.Find(tab);
                    }
                }
                else
                {
                    this.tabOrder.Remove(tab);
                    this.tabOrder.AddFirst(tab);
                }

                tab.Focus();
            }
        }

        private void TabCycleStop()
        {
            if (this.currentTabCycle != null)
            {
                this.tabOrder.Remove(this.currentTabCycle);
                this.tabOrder.AddFirst(this.currentTabCycle);
                this.currentTabCycle = null;
            }
        }

        private void TabCycleNext()
        {
            if (this.tabOrder.Count > 1)
            {
                if (this.currentTabCycle == null)
                {
                    this.currentTabCycle = this.tabOrder.First;
                }

                this.currentTabCycle = this.currentTabCycle.Next ?? this.tabOrder.First;
                this.ActiveTab = this.currentTabCycle.Value;
            }
        }

        private void TabCyclePrev()
        {
            if (this.tabOrder.Count > 1)
            {
                if (this.currentTabCycle == null)
                {
                    this.currentTabCycle = this.tabOrder.First;
                }

                this.currentTabCycle = this.currentTabCycle.Previous ?? this.tabOrder.Last;
                this.ActiveTab = this.currentTabCycle.Value;
            }
        }

        private void TabContextMenu()
        {
            if (this.ActiveTab is TabVM tab && this.tabButtons.FirstOrDefault(b => b.Tab == tab) is ButtonInfo info && info.ContextMenu is ContextMenu menu)
            {
                menu.Placement = PlacementMode.Bottom;
                menu.PlacementTarget = info.Button;
                menu.IsOpen = true;
            }
        }

        private void TabClose()
        {
            if (this.ActiveTab is TabVM tab && tab.CloseCommand is ICommand command && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        private void TabSetName()
        {
            this.ActiveTab?.Tab?.OnSetTabName();
        }

        private void TabDetach()
        {
            this.ActiveTab?.Tab?.OnDetach();
        }

        private void TabClone()
        {
            this.ActiveTab?.Tab?.OnClone();
        }

        public Api.ITabHolder AddTab(Api.ITab tab, bool activate)
        {
            TabVM tabVM = new TabVM(this.Window, this, tab);
            this.AddTab(tabVM, activate);
            return tabVM;
        }

        public Api.ITabHolder AddTab(Api.ITabSnapshot snapshot, bool activate)
        {
            TabVM tabVM = new TabVM(this.Window, this, snapshot);
            this.AddTab(tabVM, activate);
            return tabVM;
        }

        private void AddTab(TabVM tab, bool activate)
        {
            if (this.newTabIndex == ProcessWorkspace.NewTabAtEndNoActivate)
            {
                activate = false;
            }

            if (!this.tabs.Contains(tab))
            {
                int index = (this.newTabIndex < 0) ? this.tabs.Count : Math.Min(this.tabs.Count, this.newTabIndex);
                this.tabs.Insert(index, tab);
                this.tabOrder.AddFirst(tab);
                Debug.Assert(this.tabs.Count == this.tabOrder.Count);

                tab.PropertyChanged += this.OnTabPropertyChanged;
            }

            if (activate)
            {
                this.ActiveTab = tab;
            }
        }

        public void RemoveTab(Api.ITabHolder tab)
        {
            bool removingActive = (this.ActiveTab == tab);

            if (tab is TabVM tabVM && this.tabs.Remove(tabVM))
            {
                this.tabOrder.Remove(tabVM);
                Debug.Assert(this.tabs.Count == this.tabOrder.Count);

                if (removingActive)
                {
                    this.ActiveTab = this.tabOrder.First?.Value;
                    if (this.ActiveTab == null)
                    {
                        this.Window.Focus();
                    }
                }

                tabVM.PropertyChanged -= this.OnTabPropertyChanged;
                tabVM.Dispose();
            }
        }

        private void OnTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender is TabVM tab)
            {
                if (this.ActiveTab == tab)
                {
                    if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(TabVM.ViewElement))
                    {
                        this.ViewElement.ViewElement = tab.ViewElement;
                    }

                    if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(TabVM.Title))
                    {
                        this.OnPropertyChanged(nameof(this.Title));
                    }
                }

                if (this.tabButtons.FirstOrDefault(b => b.Tab == tab) is ButtonInfo info)
                {
                    if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(TabVM.ContextMenuItems))
                    {
                        info.ContextMenuUpdated = false;

                        if (info.ContextMenu is ContextMenu menu && menu.IsOpen)
                        {
                            this.UpdateContextMenu(menu);
                        }
                    }
                }
            }
        }

        public Api.ITabHolder FindTab(Api.IProcess process)
        {
            return this.tabs.Where(t => t.CreatedTab && t.Tab is ProcessTab tab && tab.Hwnd == process.Hwnd).FirstOrDefault();
        }

        public void Focus()
        {
            this.FocusActiveTab();
        }

        public void OnShowing()
        {
        }

        public void OnHiding()
        {
        }

        public void OnWindowActivated()
        {
            this.TabCycleStop();
            this.ViewElement.ProcessHostWindow?.OnActivated();
        }

        public void OnWindowDeactivated()
        {
            this.TabCycleStop();
            this.ViewElement.ProcessHostWindow?.OnDeactivated();
        }

        private void OnTabsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.TabCycleStop();
        }

        public void OnDrop(TabVM tab, int droppedIndex, bool copy)
        {
            int index = this.tabs.IndexOf(tab);
            if (index >= 0 && tab.Tab is Api.ITab apiTab)
            {
                if (copy)
                {
                    int oldTabIndex = this.newTabIndex;
                    this.newTabIndex = droppedIndex;

                    try
                    {
                        apiTab.OnClone();
                    }
                    finally
                    {
                        this.newTabIndex = oldTabIndex;
                    }
                }
                else if (index != droppedIndex)
                {
                    int finalIndex = (droppedIndex > index) ? droppedIndex - 1 : droppedIndex;
                    this.tabs.Move(index, finalIndex);
                }
            }
        }

        public Api.ITabHolder RunProcess(Api.IConsoleSettings settings)
        {
            return this.RunProcess(settings.Executable, settings.Arguments, settings.StartingDirectory, settings.TabName, settings.ThemeKeyColor);
        }

        public Api.ITabHolder RunProcess(string executable, string arguments, string startingDirectory, string tabName, Color themeKeyColor)
        {
            Api.IProcess process = this.ProcessHost?.RunProcess(executable, arguments, startingDirectory);
            if (this.FindTab(process) is TabVM tab)
            {
                if (!string.IsNullOrEmpty(tabName) && tab.Tab is ProcessTab processTab)
                {
                    processTab.RawName = tabName;
                    processTab.ThemeKeyColor = themeKeyColor;
                }

                return tab;
            }

            return null;
        }

        public Api.ITabHolder RestoreProcess(string state, string tabName, Color themeKeyColor)
        {
            Api.IProcess process = this.ProcessHost?.RestoreProcess(state);
            if (this.FindTab(process) is TabVM tab)
            {
                if (!string.IsNullOrEmpty(tabName) && tab.Tab is ProcessTab processTab)
                {
                    processTab.RawName = tabName;
                    processTab.ThemeKeyColor = themeKeyColor;
                }

                return tab;
            }

            return null;
        }

        public Api.ITabHolder CloneProcess(Api.ITab tab, string tabName, Color themeKeyColor)
        {
            if (tab is ProcessTab processTab)
            {
                Api.IProcess clone = this.ProcessHost?.CloneProcess(processTab.Process);
                if (this.FindTab(clone) is TabVM cloneTab)
                {
                    if (!string.IsNullOrEmpty(tabName) && cloneTab.Tab is ProcessTab cloneProcessTab)
                    {
                        cloneProcessTab.RawName = tabName;
                        cloneProcessTab.ThemeKeyColor = themeKeyColor;
                    }

                    return cloneTab;
                }
            }

            return null;
        }
    }
}
