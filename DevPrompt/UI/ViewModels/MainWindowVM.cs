using DevPrompt.Interop;
using DevPrompt.Settings;
using DevPrompt.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the main window (handles all menu items, etc)
    /// </summary>
    internal class MainWindowVM : PropertyNotifier, IMainWindowVM
    {
        public MainWindow Window { get; }
        public ICommand ConsoleCommand { get; }
        public ICommand GrabConsoleCommand { get; }
        public ICommand LinkCommand { get; }
        public ICommand ToolCommand { get; }
        public ICommand VisualStudioCommand { get; }
        public ICommand ClearErrorTextCommand { get; }

        private readonly ObservableCollection<ITabVM> tabs;
        private readonly LinkedList<ITabVM> tabOrder;
        private readonly Stack<Action> loadingCancelActions;
        private LinkedListNode<ITabVM> currentTabCycle;
        private int newTabIndex;
        private ITabVM activeTab;
        private string errorText;
        private DispatcherOperation savingAppSettings;
        private long loadingCount;

        private const int NewTabAtEnd = -1;
        private const int NewTabAtEndNoActivate = -2;

        public MainWindowVM(MainWindow window)
        {
            this.Window = window;
            this.Window.Activated += this.OnWindowActivated;
            this.Window.Deactivated += this.OnWindowDeactivated;
            this.tabs = new ObservableCollection<ITabVM>();
            this.tabs.CollectionChanged += this.OnTabsCollectionChanged;
            this.tabOrder = new LinkedList<ITabVM>();
            this.newTabIndex = MainWindowVM.NewTabAtEnd;
            this.loadingCancelActions = new Stack<Action>();

            this.ConsoleCommand = new DelegateCommand((object arg) => this.StartConsole((ConsoleSettings)arg));
            this.GrabConsoleCommand = new DelegateCommand((object arg) => this.GrabConsole((int)arg));
            this.LinkCommand = new DelegateCommand((object arg) => this.StartLink((LinkSettings)arg));
            this.ToolCommand = new DelegateCommand((object arg) => this.StartTool((ToolSettings)arg));
            this.VisualStudioCommand = new DelegateCommand((object arg) => this.StartVisualStudio((VisualStudioSetup.Instance)arg));
            this.ClearErrorTextCommand = new DelegateCommand((object arg) => this.ClearErrorText());

            this.AppSettings.PropertyChanged += this.OnAppSettingsPropertyChanged;
            this.AppSettings.ObservableConsoles.CollectionChanged += this.OnAppSettingsCollectionChanged;
            this.AppSettings.ObservableGrabConsoles.CollectionChanged += this.OnAppSettingsCollectionChanged;
            this.AppSettings.ObservableLinks.CollectionChanged += this.OnAppSettingsCollectionChanged;
            this.AppSettings.ObservableTools.CollectionChanged += this.OnAppSettingsCollectionChanged;
        }

        private void OnAppSettingsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.OnAppSettingsPropertyChanged(sender, null);
        }

        private async void OnAppSettingsPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (this.Window.IsVisible)
            {
                await this.SaveAppSettings();
            }
        }

        public Dispatcher Dispatcher
        {
            get
            {
                return this.Window.Dispatcher;
            }
        }

        public IProcessHost ProcessHost
        {
            get
            {
                return this.Window.ProcessHostWindow?.ProcessHost;
            }
        }

        public AppSettings AppSettings
        {
            get
            {
                return this.Window.AppSettings;
            }
        }

        private async Task SaveAppSettings(string path = null)
        {
            if (this.savingAppSettings == null)
            {
                Action action = async () =>
                {
                    this.savingAppSettings = null;

                    if (await this.AppSettings.Save(this.Window.App, path) is Exception exception)
                    {
                        this.SetError(exception);
                    }
                };

                this.savingAppSettings = this.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action);
            }

            await this.savingAppSettings;
        }

        public ICommand DetachAndExitCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    foreach (ITabVM tab in this.tabs.ToArray())
                    {
                        if (tab.DetachCommand != null && tab.DetachCommand.CanExecute(null))
                        {
                            tab.DetachCommand.Execute(null);
                        }
                    }

                    this.Window.Close();
                });
            }
        }

        public ICommand ExitCommand
        {
            get
            {
                return new DelegateCommand(() => this.Window.Close());
            }
        }

        public ICommand VisualStudioInstallerCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    this.StartExternalProcess(VisualStudioSetup.InstallerPath);
                });
            }
        }

        public ICommand VisualStudioDogfoodCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    this.StartExternalProcess(VisualStudioSetup.DogfoodInstallerPath);
                });
            }
        }

        public ICommand InstallVisualStudioBranchCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    InstallBranchDialog dialog = new InstallBranchDialog()
                    {
                        Owner = this.Window
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.StartExternalProcess(dialog.ViewModel.Hyperlink);
                    }
                });
            }
        }

        public ICommand CustomizeConsolesCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    CustomizeConsolesDialog dialog = new CustomizeConsolesDialog(this.AppSettings.Consoles)
                    {
                        Owner = this.Window
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.AppSettings.Consoles.Clear();

                        foreach (ConsoleSettings settings in dialog.Settings)
                        {
                            this.AppSettings.Consoles.Add(settings.Clone());
                        }
                    }
                });
            }
        }

        public ICommand SettingsImportCommand
        {
            get
            {
                return new DelegateCommand(async () =>
                {
                    OpenFileDialog dialog = new OpenFileDialog
                    {
                        Title = "Import Settings",
                        Filter = "XML Files|*.xml",
                        DefaultExt = "xml",
                        CheckFileExists = true
                    };

                    if (dialog.ShowDialog(this.Window) == true)
                    {
                        AppSettings settings = null;
                        try
                        {
                            settings = await AppSettings.UnsafeLoad(this.Window.App, dialog.FileName);
                        }
                        catch (Exception exception)
                        {
                            this.SetError(exception);
                        }

                        if (settings != null)
                        {
                            SettingsImportDialog dialog2 = new SettingsImportDialog(settings)
                            {
                                Owner = this.Window
                            };

                            if (dialog2.ShowDialog() == true)
                            {
                                dialog2.ViewModel.Import(this.AppSettings);
                            }
                        }
                    }
                });
            }
        }

        public ICommand SettingsExportCommand
        {
            get
            {
                return new DelegateCommand(async () =>
                {
                    OpenFileDialog dialog = new OpenFileDialog
                    {
                        Title = "Export Settings",
                        Filter = "XML Files|*.xml",
                        DefaultExt = "xml",
                        CheckPathExists = true,
                        CheckFileExists = false,
                        ValidateNames = true
                    };

                    if (dialog.ShowDialog(this.Window) == true)
                    {
                        await this.SaveAppSettings(dialog.FileName);
                    }
                });
            }
        }

        public ICommand CustomizeConsoleGrabCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    CustomizeGrabConsolesDialog dialog = new CustomizeGrabConsolesDialog(this.AppSettings.GrabConsoles)
                    {
                        Owner = this.Window
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.AppSettings.GrabConsoles.Clear();

                        foreach (GrabConsoleSettings settings in dialog.Settings)
                        {
                            this.AppSettings.GrabConsoles.Add(settings.Clone());
                        }
                    }
                });
            }
        }

        public ICommand CustomizeToolsCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    CustomizeToolsDialog dialog = new CustomizeToolsDialog(this.AppSettings.Tools)
                    {
                        Owner = this.Window
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.AppSettings.Tools.Clear();

                        foreach (ToolSettings settings in dialog.Settings)
                        {
                            this.AppSettings.Tools.Add(settings.Clone());
                        }
                    }
                });
            }
        }

        public ICommand CustomizeLinksCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    CustomizeLinksDialog dialog = new CustomizeLinksDialog(this.AppSettings.Links)
                    {
                        Owner = this.Window
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.AppSettings.Links.Clear();

                        foreach (LinkSettings settings in dialog.Settings)
                        {
                            this.AppSettings.Links.Add(settings.Clone());
                        }
                    }
                });
            }
        }

        public ICommand AboutCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    AboutDialog dialog = new AboutDialog(this)
                    {
                        Owner = this.Window
                    };

                    dialog.ShowDialog();
                });
            }
        }

        public string WindowTitle
        {
            get
            {
                string intro = Program.IsElevated ? "[Dev Admin]" : "[Dev]";
                string title = this.ActiveTab?.Title;

                if (!string.IsNullOrEmpty(title))
                {
                    return $"{intro} {title}";
                }

                return intro;
            }
        }

        public IReadOnlyList<ITabVM> Tabs
        {
            get
            {
                return this.tabs;
            }
        }

        public ITabVM ActiveTab
        {
            get
            {
                return this.activeTab;
            }

            set
            {
                ITabVM oldTab = this.activeTab;
                if (this.SetPropertyValue(ref this.activeTab, value))
                {
                    if (this.activeTab != null)
                    {
                        if (!this.tabs.Contains(this.activeTab))
                        {
                            this.AddTab(this.activeTab, activate: false);
                        }

                        this.activeTab.Active = true;
                    }

                    if (oldTab != null)
                    {
                        oldTab.Active = false;
                    }

                    this.Window.ViewElement = this.activeTab?.ViewElement;

                    this.OnPropertyChanged(nameof(this.HasActiveTab));
                    this.OnPropertyChanged(nameof(this.WindowTitle));
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
            if (this.ActiveTab != null)
            {
                if (this.currentTabCycle != null)
                {
                    if (this.currentTabCycle.Value != this.ActiveTab)
                    {
                        this.currentTabCycle = this.tabOrder.Find(this.ActiveTab);
                    }
                }
                else
                {
                    this.tabOrder.Remove(this.ActiveTab);
                    this.tabOrder.AddFirst(this.ActiveTab);
                }

                this.ActiveTab.Focus();
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

        public void AddTab(ITabVM tab, bool activate)
        {
            this.ClearErrorText();

            if (this.newTabIndex == MainWindowVM.NewTabAtEndNoActivate)
            {
                activate = false;
            }

            int index = (this.newTabIndex < 0) ? this.tabs.Count : Math.Min(this.tabs.Count, this.newTabIndex);
            this.tabs.Insert(index, tab);
            this.tabOrder.AddFirst(tab);
            Debug.Assert(this.tabs.Count == this.tabOrder.Count);

            if (activate || this.ActiveTab == null)
            {
                this.ActiveTab = tab;
            }

            tab.PropertyChanged += this.OnTabPropertyChanged;
        }

        public void RemoveTab(ITabVM tab)
        {
            bool removingActive = (this.ActiveTab == tab);

            this.tabs.Remove(tab);
            this.tabOrder.Remove(tab);
            Debug.Assert(this.tabs.Count == this.tabOrder.Count);

            if (removingActive)
            {
                this.ActiveTab = this.tabOrder.First?.Value;
            }

            tab.PropertyChanged -= this.OnTabPropertyChanged;

            if (tab is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void OnTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender is ITabVM tab && this.ActiveTab == tab)
            {
                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(ITabVM.ViewElement))
                {
                    this.Window.ViewElement = tab.ViewElement;
                }

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(ITabVM.Title))
                {
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }
            }
        }

        private void StartConsole(ConsoleSettings settings)
        {
            IProcess process = this.ProcessHost?.RunProcess(
                settings.Executable,
                settings.ExpandedArguments,
                settings.ExpandedStartingDirectory);

            if (this.FindProcess(process) is ProcessVM tab)
            {
                tab.TabName = settings.TabName;
            }
        }

        private void GrabConsole(int processId)
        {
            this.Window.NativeApp?.GrabProcess(processId);
        }

        public async void RunStartupConsoles()
        {
            AppSnapshot snapshot = await AppSnapshot.Load(this.Window.App, AppSnapshot.DefaultPath);

            try
            {
                this.newTabIndex = MainWindowVM.NewTabAtEndNoActivate;

                foreach (ITabSnapshot tabSnapshot in snapshot.Tabs)
                {
                    if (tabSnapshot.Restore(this) is ITabVM tab)
                    {
                        if (!this.tabs.Contains(tab))
                        {
                            this.AddTab(tab, false);
                        }

                        if (snapshot.ActiveTabIndex >= 0 &&
                            snapshot.ActiveTabIndex < snapshot.Tabs.Count &&
                            snapshot.Tabs[snapshot.ActiveTabIndex] == tabSnapshot)
                        {
                            this.ActiveTab = tab;
                        }
                    }
                }

                if (this.tabs.Count == 0)
                {
                    foreach (ConsoleSettings settings in this.AppSettings.Consoles)
                    {
                        if (settings.RunAtStartup)
                        {
                            this.StartConsole(settings);
                        }
                    }
                }
            }
            finally
            {
                this.newTabIndex = MainWindowVM.NewTabAtEnd;
            }
        }

        public void SetError(Exception exception, string text = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                this.ErrorText = exception?.Message ?? string.Empty;
            }
            else
            {
                this.ErrorText = text;
            }
        }

        public string ErrorText
        {
            get
            {
                return this.errorText ?? string.Empty;
            }

            set
            {
                if (this.SetPropertyValue(ref this.errorText, value))
                {
                    this.OnPropertyChanged(nameof(this.HasErrorText));
                }
            }
        }

        public bool HasErrorText
        {
            get
            {
                return !string.IsNullOrEmpty(this.ErrorText);
            }
        }

        public void OnDrop(ITabVM tab, int droppedIndex, bool copy)
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

        public bool Loading
        {
            get
            {
                return Interlocked.Read(ref this.loadingCount) != 0;
            }
        }

        public bool NotLoading
        {
            get
            {
                return !this.Loading;
            }
        }

        public IDisposable BeginLoading(Action cancelAction, string text)
        {
            this.loadingCancelActions.Push(cancelAction);

            if (Interlocked.Increment(ref this.loadingCount) == 1)
            {
                this.OnPropertyChanged(nameof(this.Loading));
            }

            return new DelegateDisposable(() =>
            {
                this.loadingCancelActions.Pop();

                if (Interlocked.Decrement(ref this.loadingCount) == 0)
                {
                    this.OnPropertyChanged(nameof(this.Loading));
                }
            });
        }

        public void CancelLoading()
        {
            Action[] actions = this.loadingCancelActions.ToArray();
            this.loadingCancelActions.Clear();

            foreach (Action action in actions)
            {
                action?.Invoke();
            }
        }

        public ProcessVM FindProcess(IProcess process)
        {
            IntPtr processHwnd = process?.GetWindow() ?? IntPtr.Zero;
            return this.tabs.OfType<ProcessVM>().FirstOrDefault(p => p.Hwnd == processHwnd);
        }

        ITabVM IMainWindowVM.RestoreConsoleTab(string state)
        {
            IProcess process = this.ProcessHost?.RestoreProcess(state);
            return this.FindProcess(process);
        }

        private void ClearErrorText()
        {
            this.SetError(null);
        }

        private void StartLink(LinkSettings settings)
        {
            this.StartExternalProcess(settings.Address);
        }

        private void StartTool(ToolSettings settings)
        {
            this.StartExternalProcess(settings.ExpandedCommand, settings.ExpandedArguments);
        }

        private void StartVisualStudio(VisualStudioSetup.Instance instance)
        {
            this.StartExternalProcess(instance.ProductPath);
        }

        public void StartExternalProcess(string path, string arguments = null)
        {
            this.ClearErrorText();

            try
            {
                if (string.IsNullOrEmpty(arguments))
                {
                    Process.Start(new ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(path, arguments);
                }
            }
            catch (Exception ex)
            {
                this.SetError(ex, $"Error: Failed to start \"{path}\"");
            }
        }

        private void OnWindowDeactivated(object sender, EventArgs args)
        {
            this.TabCycleStop();
        }

        private void OnWindowActivated(object sender, EventArgs args)
        {
            this.TabCycleStop();
        }

        private void OnTabsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.TabCycleStop();
        }
    }
}
