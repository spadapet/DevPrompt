using DevPrompt.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DevPrompt.ProcessWorkspace
{
    internal class ProcessWorkspace : Api.PropertyNotifier, Api.IProcessWorkspace
    {
        public Api.IWindow Window { get; }
        public Guid Id => Api.Constants.ProcessWorkspaceId;
        public string Name => ProcessWorkspace.StaticName;
        public string Tooltip => ProcessWorkspace.StaticTooltip;
        public string Title => this.ActiveTab?.Title ?? this.Name;
        public Api.IWorkspaceSnapshot Snapshot => new ProcessWorkspaceSnapshot(this);
        public IEnumerable<Api.ITabVM> Tabs => this.tabs;
        public Api.IProcessHost ProcessHost => this.ViewElement.ProcessHostWindow?.ProcessHost;

        public static string StaticName => "Command Prompts";
        public static string StaticTooltip => string.Empty;

        private readonly List<Button> tabButtons;
        private readonly ObservableCollection<Api.ITabVM> tabs;
        private readonly LinkedList<Api.ITabVM> tabOrder;
        private LinkedListNode<Api.ITabVM> currentTabCycle;
        private Api.ITabVM activeTab;
        private int newTabIndex;
        private ProcessWorkspaceControl viewElement;
        private ProcessWorkspaceSnapshot initialSnapshot;

        private const int NewTabAtEnd = -1;
        private const int NewTabAtEndNoActivate = -2;

        public ProcessWorkspace(Api.IWindow window, ProcessWorkspaceSnapshot snapshot = null)
        {
            this.Window = window;
            this.tabButtons = new List<Button>();
            this.tabs = new ObservableCollection<Api.ITabVM>();
            this.tabs.CollectionChanged += this.OnTabsCollectionChanged;
            this.tabOrder = new LinkedList<Api.ITabVM>();
            this.newTabIndex = ProcessWorkspace.NewTabAtEnd;
            this.initialSnapshot = snapshot;
        }

        private void InitTabs(ProcessWorkspaceSnapshot snapshot)
        {
            try
            {
                this.newTabIndex = ProcessWorkspace.NewTabAtEndNoActivate;

                foreach (Api.ITabSnapshot tabSnapshot in snapshot?.Tabs ?? Enumerable.Empty<Api.ITabSnapshot>())
                {
                    Api.TabVM tab = new Api.TabVM(this.Window, this, tabSnapshot);
                    this.AddTab(tab, false);

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

                if (this.ActiveTab == null && this.tabs.Count > 0)
                {
                    this.ActiveTab = this.tabs[0];
                }
            }
            finally
            {
                this.newTabIndex = ProcessWorkspace.NewTabAtEnd;
            }
        }

        public void AddTabButton(Button button)
        {
            Debug.Assert(button != null && !this.tabButtons.Contains(button));
            this.tabButtons.Add(button);
        }

        public void RemoveTabButton(Button button)
        {
            Debug.Assert(button != null && this.tabButtons.Contains(button));
            this.tabButtons.Remove(button);
        }

        IEnumerable<MenuItem> Api.IWorkspace.MenuItems => null;
        UIElement Api.IWorkspace.ViewElement => this.ViewElement;

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

        private void OnViewElementLoaded(object sender, RoutedEventArgs args)
        {
            this.viewElement.Loaded -= this.OnViewElementLoaded;

            ProcessWorkspaceSnapshot snapshot = this.initialSnapshot;
            this.initialSnapshot = null;

            this.InitTabs(snapshot);
        }

        public Api.ITabVM ActiveTab
        {
            get
            {
                return this.activeTab;
            }

            set
            {
                Api.ITabVM oldTab = this.ActiveTab;
                if (this.SetPropertyValue(ref this.activeTab, value))
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

                    this.ViewElement.ViewElement = this.ActiveTab?.ViewElement;

                    this.OnPropertyChanged(nameof(this.HasActiveTab));
                    this.OnPropertyChanged(nameof(this.Title));
                }

                this.FocusActiveTab();
            }
        }

        public bool HasActiveTab
        {
            get
            {
                return this.ActiveTab != null;
            }
        }

        public void FocusActiveTab()
        {
            if (this.ActiveTab is Api.ITabVM tab)
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

        public void TabCycleStop()
        {
            if (this.currentTabCycle != null)
            {
                this.tabOrder.Remove(this.currentTabCycle);
                this.tabOrder.AddFirst(this.currentTabCycle);
                this.currentTabCycle = null;
            }
        }

        public void TabCycleNext()
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

        public void TabCyclePrev()
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

        public void TabContextMenu()
        {
            if (this.ActiveTab is Api.ITabVM tab)
            {
                foreach (Button button in this.tabButtons)
                {
                    if (button.DataContext == tab && button.ContextMenu is ContextMenu menu)
                    {
                        menu.Placement = PlacementMode.Bottom;
                        menu.PlacementTarget = button;
                        menu.IsOpen = true;
                        break;
                    }
                }
            }
        }

        public void AddTab(Api.ITabVM tab, bool activate)
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

        public void RemoveTab(Api.ITabVM tab)
        {
            bool removingActive = (this.ActiveTab == tab);

            if (this.tabs.Remove(tab))
            {
                this.tabOrder.Remove(tab);
                Debug.Assert(this.tabs.Count == this.tabOrder.Count);

                if (removingActive)
                {
                    this.ActiveTab = this.tabOrder.First?.Value;
                    if (this.ActiveTab == null)
                    {
                        this.Window.Window.Focus();
                    }
                }

                tab.PropertyChanged -= this.OnTabPropertyChanged;
                tab.Dispose();
            }
        }

        private void OnTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender is Api.ITabVM tab && this.ActiveTab == tab)
            {
                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITabVM.ViewElement))
                {
                    this.ViewElement.ViewElement = tab.ViewElement;
                }

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITabVM.Title))
                {
                    this.OnPropertyChanged(nameof(this.Title));
                }
            }
        }

        public Api.ITabVM FindTab(Api.IProcess process)
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

        public void OnDrop(Api.ITabVM tab, int droppedIndex, bool copy)
        {
            int index = this.tabs.IndexOf(tab);
            if (index >= 0)
            {
                if (copy && tab.CloneCommand?.CanExecute(null) == true)
                {
                    int oldTabIndex = this.newTabIndex;
                    this.newTabIndex = droppedIndex;

                    try
                    {
                        tab.CloneCommand.Execute(null);
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

        public Api.ITabVM RunProcess(Api.IConsoleSettings settings)
        {
            return this.RunProcess(settings.Executable, settings.Arguments, settings.StartingDirectory, settings.TabName);
        }

        public Api.ITabVM RunProcess(string executable, string arguments, string startingDirectory, string tabName)
        {
            Api.IProcess process = this.ProcessHost?.RunProcess(executable, arguments, startingDirectory);
            if (this.FindTab(process) is Api.ITabVM tab)
            {
                if (!string.IsNullOrEmpty(tabName) && tab.Tab is ProcessTab processTab)
                {
                    processTab.RawName = tabName;
                }

                return tab;
            }

            return null;
        }

        public Api.ITabVM RestoreProcess(string state, string tabName)
        {
            Api.IProcess process = this.ProcessHost?.RestoreProcess(state);
            if (this.FindTab(process) is Api.ITabVM tab)
            {
                if (!string.IsNullOrEmpty(tabName) && tab.Tab is ProcessTab processTab)
                {
                    processTab.RawName = tabName;
                }

                return tab;
            }

            return null;
        }

        public Api.ITabVM CloneProcess(Api.ITab tab, string tabName)
        {
            if (tab is ProcessTab processTab)
            {
                Api.IProcess clone = this.ProcessHost?.CloneProcess(processTab.Process);
                if (this.FindTab(clone) is Api.ITabVM cloneTab)
                {
                    if (!string.IsNullOrEmpty(tabName) && cloneTab.Tab is ProcessTab cloneProcessTab)
                    {
                        cloneProcessTab.RawName = tabName;
                    }

                    return cloneTab;
                }
            }

            return null;
        }
    }
}
