using DevPrompt.Interop;
using DevPrompt.Settings;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevPrompt.UI
{
    /// <summary>
    /// View model for the main window (handles all menu items, etc)
    /// </summary>
    internal class MainWindowVM : PropertyNotifier
    {
        public MainWindow Window { get; }
        public ICommand ConsoleCommand { get; }
        public ICommand GrabConsoleCommand { get; }
        public ICommand LinkCommand { get; }
        public ICommand ToolCommand { get; }
        public ICommand VisualStudioCommand { get; }
        public ICommand ClearErrorTextCommand { get; }

        private readonly ObservableCollection<ProcessVM> processes;
        private readonly LinkedList<ProcessVM> tabOrder;
        private LinkedListNode<ProcessVM> currentTabCycle;
        private int newTabIndex;
        private ProcessVM activeProcess;
        private string errorText;
        private bool saveAppSettingsPending;

        /// <summary>
        /// For designer
        /// </summary>
        public MainWindowVM()
        {
        }

        public MainWindowVM(MainWindow window)
        {
            this.Window = window;
            this.Window.Activated += this.OnWindowActivated;
            this.Window.Deactivated += this.OnWindowDeactivated;
            this.processes = new ObservableCollection<ProcessVM>();
            this.processes.CollectionChanged += this.OnProcessesCollectionChanged;
            this.tabOrder = new LinkedList<ProcessVM>();
            this.newTabIndex = -1;

            this.ConsoleCommand = new DelegateCommand((object arg) => this.StartConsole((ConsoleSettings)arg));
            this.GrabConsoleCommand = new DelegateCommand((object arg) => this.GrabConsole((int)arg));
            this.LinkCommand = new DelegateCommand((object arg) => this.StartLink((LinkSettings)arg));
            this.ToolCommand = new DelegateCommand((object arg) => this.StartTool((ToolSettings)arg));
            this.VisualStudioCommand = new DelegateCommand((object arg) => this.StartVisualStudio((VisualStudioSetup.Instance)arg));
            this.ClearErrorTextCommand = new DelegateCommand((object arg) => this.ClearErrorText());
            this.AppSettings.PropertyChanged += this.OnAppSettingsPropertyChanged;
        }

        private void OnAppSettingsPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // Don't want to lose any setting changes if the app dies, so make sure they are saved
            if (!this.saveAppSettingsPending && this.Window.IsVisible)
            {
                this.saveAppSettingsPending = true;

                Action action = () =>
                {
                    this.saveAppSettingsPending = false;
                    this.AppSettings.Save();
                };

                this.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action);
            }
        }

        private IProcessHost ProcessHost
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
                    this.StartProcess(VisualStudioSetup.InstallerPath);
                });
            }
        }

        public ICommand VisualStudioDogfoodCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    this.StartProcess(VisualStudioSetup.DogfoodInstallerPath);
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
                        this.StartProcess(dialog.ViewModel.Hyperlink);
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

                        this.AppSettings.Save();
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

                        this.AppSettings.Save();
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

                        this.AppSettings.Save();
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

                        this.AppSettings.Save();
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
                    AboutDialog dialog = new AboutDialog()
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
                string title = this.ActiveProcess?.Title;

                if (!string.IsNullOrEmpty(title))
                {
                    return $"{intro} {title}";
                }

                return intro;
            }
        }

        public IList<ProcessVM> Processes
        {
            get
            {
                return this.processes;
            }
        }

        public ProcessVM ActiveProcess
        {
            get
            {
                return this.activeProcess;
            }

            set
            {
                ProcessVM oldProcess = this.activeProcess;
                if (this.SetPropertyValue(ref this.activeProcess, value))
                {
                    if (this.activeProcess != null)
                    {
                        this.activeProcess.InternalActive = true;
                    }

                    if (oldProcess != null)
                    {
                        oldProcess.InternalActive = false;
                    }

                    this.OnPropertyChanged(nameof(this.HasActiveProcess));
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }

                this.FocusActiveProcess();
            }
        }

        public bool HasActiveProcess
        {
            get
            {
                return this.ActiveProcess != null;
            }
        }

        public void FocusActiveProcess()
        {
            if (this.activeProcess != null)
            {
                if (this.currentTabCycle != null)
                {
                    if (this.currentTabCycle.Value != this.activeProcess)
                    {
                        this.currentTabCycle = this.tabOrder.Find(this.activeProcess);
                    }
                }
                else
                {
                    this.tabOrder.Remove(this.activeProcess);
                    this.tabOrder.AddFirst(this.activeProcess);
                }

                this.activeProcess.Process.Focus();
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
                this.ActiveProcess = this.currentTabCycle.Value;
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
                this.ActiveProcess = this.currentTabCycle.Value;
            }
        }

        /// <summary>
        /// Notification from the native app
        /// </summary>
        public void OnProcessOpening(IProcess process, bool activate, string path)
        {
            ProcessVM processVM = new ProcessVM(this, process)
            {
                TabName = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "Tab"
            };

            foreach (GrabConsoleSettings grab in this.AppSettings.GrabConsoles)
            {
                if (grab.CanGrab(path))
                {
                    processVM.TabName = grab.TabName;
                    break;
                }
            }

            int index = (this.newTabIndex < 0) ? this.processes.Count : Math.Min(this.processes.Count, this.newTabIndex);
            this.processes.Insert(index, processVM);

            this.tabOrder.AddFirst(processVM);
            Debug.Assert(this.processes.Count == this.tabOrder.Count);

            if (activate || this.ActiveProcess == null)
            {
                this.ActiveProcess = processVM;
            }
        }

        /// <summary>
        /// Notification from the native app
        /// </summary>
        public void OnProcessClosing(IProcess process)
        {
            IntPtr processHwnd = process.GetWindow();
            ProcessVM processVM = this.processes.FirstOrDefault(p => p.Hwnd == processHwnd);
            if (processVM != null)
            {
                bool removingActive = (this.ActiveProcess == processVM);

                this.processes.Remove(processVM);
                this.tabOrder.Remove(processVM);
                Debug.Assert(this.processes.Count == this.tabOrder.Count);

                if (removingActive)
                {
                    this.ActiveProcess = this.tabOrder.First?.Value;
                }
            }
        }

        /// <summary>
        /// Notification from the native app
        /// </summary>
        public void OnProcessEnvChanged(IProcess process, string env)
        {
            IntPtr processHwnd = process.GetWindow();
            ProcessVM processVM = this.processes.FirstOrDefault(p => p.Hwnd == processHwnd);
            if (processVM != null)
            {
                processVM.Env = env;
            }
        }

        /// <summary>
        /// Notification from the native app
        /// </summary>
        public void OnProcessTitleChanged(IProcess process, string title)
        {
            IntPtr processHwnd = process.GetWindow();
            ProcessVM processVM = this.processes.FirstOrDefault(p => p.Hwnd == processHwnd);
            if (processVM != null)
            {
                processVM.Title = title;

                if (this.ActiveProcess == processVM)
                {
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }
            }
        }

        public void CloneProcess(ProcessVM process)
        {
            this.ClearErrorText();

            if (process != null)
            {
                IProcess processClone = this.ProcessHost?.CloneProcess(process.Process);
                IntPtr hwndClone = processClone?.GetWindow() ?? IntPtr.Zero;
                ProcessVM clone = this.processes.FirstOrDefault(p => p.Hwnd == hwndClone);
                if (clone != null)
                {
                    clone.TabName = process.TabName;
                }
            }
        }

        public async void RunStartupConsoles()
        {
            AppSnapshot snapshot = await AppSnapshot.Load(AppSnapshot.DefaultPath);

            foreach (ConsoleSnapshot console in snapshot.Consoles)
            {
                this.RestoreConsole(console);
            }

            if (this.Processes.Count == 0)
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

        public void OnDrop(ProcessVM process, int droppedIndex, bool copy)
        {
            int index = this.processes.IndexOf(process);
            if (index >= 0)
            {
                if (copy)
                {
                    int oldTabIndex = this.newTabIndex;
                    this.newTabIndex = droppedIndex;

                    try
                    {
                        this.CloneProcess(process);
                    }
                    finally
                    {
                        this.newTabIndex = oldTabIndex;
                    }
                }
                else if (index != droppedIndex)
                {
                    int finalIndex = (droppedIndex > index) ? droppedIndex - 1 : droppedIndex;
                    this.processes.Move(index, finalIndex);
                }
            }
        }

        private void ClearErrorText()
        {
            this.ErrorText = null;
        }

        private void StartConsole(ConsoleSettings settings)
        {
            this.ClearErrorText();

            IProcess processNew = this.ProcessHost?.RunProcess(
                settings.Executable,
                settings.ExpandedArguments,
                settings.ExpandedStartingDirectory);
            IntPtr hwndNew = processNew?.GetWindow() ?? IntPtr.Zero;

            ProcessVM process = this.processes.FirstOrDefault(p => p.Hwnd == hwndNew);
            if (process != null)
            {
                process.TabName = settings.TabName;
            }
        }

        private void RestoreConsole(ConsoleSnapshot console)
        {
            IProcess processNew = this.ProcessHost?.RestoreProcess(console.State);
            IntPtr hwndNew = processNew?.GetWindow() ?? IntPtr.Zero;

            ProcessVM process = this.processes.FirstOrDefault(p => p.Hwnd == hwndNew);
            if (process != null)
            {
                process.TabName = console.TabName;
            }
        }

        private void GrabConsole(int processId)
        {
            App.Current.NativeApp?.GrabProcess(processId);
        }

        private void StartLink(LinkSettings settings)
        {
            this.StartProcess(settings.Address);
        }

        private void StartTool(ToolSettings settings)
        {
            this.StartProcess(settings.ExpandedCommand, settings.ExpandedArguments);
        }

        private void StartVisualStudio(VisualStudioSetup.Instance instance)
        {
            this.StartProcess(instance.ProductPath);
        }

        private void StartProcess(string path, string arguments = null)
        {
            this.ClearErrorText();

            try
            {
                if (string.IsNullOrEmpty(arguments))
                {
                    Process.Start(path);
                }
                else
                {
                    Process.Start(path, arguments);
                }
            }
            catch
            {
                this.ErrorText = $"Error: Failed to start \"{path}\"";
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

        private void OnProcessesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.TabCycleStop();
        }
    }
}
