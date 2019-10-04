using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.UI.Settings;
using DevPrompt.Utility;
using DevPrompt.Utility.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the main window (handles all menu items, etc)
    /// </summary>
    internal class MainWindowVM : PropertyNotifier, Api.IWindow
    {
        public MainWindow Window { get; }
        public App App => this.Window.App;
        public AppSettings AppSettings => this.App.Settings;
        Api.IApp Api.IWindow.App => this.App;
        IntPtr Api.IWindow.Handle => new WindowInteropHelper(this.Window).Handle;
        Api.IProgressBar Api.IWindow.ProgressBar => this.Window.progressBar;
        Api.IInfoBar Api.IWindow.InfoBar => this.Window.infoBar;

        public ICommand ConsoleCommand { get; }
        public ICommand GrabConsoleCommand { get; }
        public ICommand LinkCommand { get; }
        public ICommand ToolCommand { get; }
        public ICommand VisualStudioCommand { get; }
        public ICommand QuickStartConsoleCommand { get; }

        private ObservableCollection<IWorkspaceVM> workspaces;
        private Dictionary<IWorkspaceVM, MenuItem[]> workspaceMenuItems;
        private IMultiValueConverter workspaceMenuItemVisibilityConverter;
        private IWorkspaceVM activeWorkspace;

        public MainWindowVM(MainWindow window)
        {
            this.Window = window;
            this.Window.Closed += this.OnWindowClosed;
            this.Window.Activated += this.OnWindowActivated;
            this.Window.Deactivated += this.OnWindowDeactivated;

            this.ConsoleCommand = new DelegateCommand((object arg) => this.StartConsole((ConsoleSettings)arg));
            this.GrabConsoleCommand = new DelegateCommand((object arg) => this.App.GrabProcess((int)arg));
            this.LinkCommand = new DelegateCommand((object arg) => this.StartLink((LinkSettings)arg));
            this.ToolCommand = new DelegateCommand((object arg) => this.StartTool((ToolSettings)arg));
            this.VisualStudioCommand = new DelegateCommand((object arg) => this.StartVisualStudio((VisualStudioSetup.Instance)arg));
            this.QuickStartConsoleCommand = new DelegateCommand((object arg) => this.QuickStartConsole((int)arg));

            this.workspaces = new ObservableCollection<IWorkspaceVM>();
            this.workspaceMenuItems = new Dictionary<IWorkspaceVM, MenuItem[]>();
            this.workspaceMenuItemVisibilityConverter = new DelegateMultiConverter(this.ConvertWorkspaceMenuItemVisibility);
        }

        private void Dispose()
        {
            this.Window.Closed -= this.OnWindowClosed;
            this.Window.Activated -= this.OnWindowActivated;
            this.Window.Deactivated -= this.OnWindowDeactivated;
        }

        private void OnWindowClosed(object sender, EventArgs args)
        {
            this.Dispose();
        }

        private void OnWindowActivated(object sender, EventArgs args)
        {
            this.ActiveWorkspace?.Workspace.OnWindowActivated();
        }

        private void OnWindowDeactivated(object sender, EventArgs args)
        {
            this.ActiveWorkspace?.Workspace.OnWindowDeactivated();
        }

        public ICommand ExitCommand => new DelegateCommand(() => this.Window.Close());

        public ICommand VisualStudioInstallerCommand => new DelegateCommand(() =>
        {
            this.RunExternalProcess(VisualStudioSetup.InstallerPath);
        });

        public ICommand VisualStudioDogfoodCommand => new DelegateCommand(() =>
        {
            this.RunExternalProcess(VisualStudioSetup.DogfoodInstallerPath);
        });

        public ICommand InstallVisualStudioBranchCommand => new DelegateCommand(() =>
        {
            InstallBranchDialog dialog = new InstallBranchDialog()
            {
                Owner = this.Window
            };

            if (dialog.ShowDialog() == true)
            {
                this.RunExternalProcess(dialog.ViewModel.Hyperlink);
            }
        });

        private void ShowSettingsDialog(SettingsTabType tab)
        {
            using (SettingsDialog dialog = new SettingsDialog(this.Window, this.AppSettings, tab))
            {
                if (dialog.ShowDialog() == true)
                {
                    this.AppSettings.CopyFrom(dialog.ViewModel.Settings);

                    if (this.AppSettings.PluginsChanged)
                    {
                        this.AppSettings.PluginsChanged = false;

                        if (MessageBox.Show(
                            this.Window,
                            Resources.Plugins_RestartText,
                            Resources.Plugins_RestartCaption,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            MessageBoxResult.No) == MessageBoxResult.Yes)
                        {
                            this.Window.CloseAndRestart(justReopenWindow: false);
                        }
                    }
                }
            }
        }

        public ICommand SettingsCommand => new DelegateCommand(() =>
        {
            this.ShowSettingsDialog(SettingsTabType.Default);
        });

        public ICommand PluginsCommand => new DelegateCommand(() =>
        {
            this.ShowSettingsDialog(SettingsTabType.Plugins);
        });

        public ICommand CustomizeConsoleGrabCommand => new DelegateCommand(() =>
        {
            this.ShowSettingsDialog(SettingsTabType.Grab);
        });

        public ICommand CustomizeToolsCommand => new DelegateCommand(() =>
        {
            this.ShowSettingsDialog(SettingsTabType.Tools);
        });

        public ICommand CustomizeLinksCommand => new DelegateCommand(() =>
        {
            this.ShowSettingsDialog(SettingsTabType.Links);
        });

        public ICommand ReportAnIssueCommand => new DelegateCommand(() =>
        {
            this.RunExternalProcess(Resources.About_IssuesLink);
        });

        public ICommand AboutCommand => new DelegateCommand(() =>
        {
            AboutDialog dialog = new AboutDialog(this)
            {
                Owner = this.Window
            };

            dialog.ShowDialog();
        });

        public ICommand SetActiveTabNameCommand => new DelegateCommand(() =>
        {
            if (this.ActiveTabWorkspace is Api.IProcessWorkspace workspace)
            {
                workspace.SetActiveTabName();
            }
        });

        public ICommand CloseActiveTabCommand => new DelegateCommand(() =>
        {
            if (this.ActiveTabWorkspace is Api.ITabWorkspace workspace)
            {
                workspace.TabClose();
            }
        });

        public ICommand DetachActiveTabCommand => new DelegateCommand(() =>
        {
            if (this.ActiveTabWorkspace is Api.IProcessWorkspace workspace)
            {
                workspace.DetachActiveTab();
            }
        });

        public ICommand CloneActiveTabCommand => new DelegateCommand(() =>
        {
            if (this.ActiveTabWorkspace is Api.IProcessWorkspace workspace)
            {
                workspace.CloneActiveTab();
            }
        });

        public ICommand TabCycleNextCommand => new DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.TabCycleNext();
        });

        public ICommand TabCyclePrevCommand => new DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.TabCyclePrev();
        });

        public ICommand ContextMenuCommand => new DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.TabContextMenu();
        });

        public string WindowTitle
        {
            get
            {
                string intro = Program.IsElevated ? Resources.Window_TitlePrefixAdmin : Resources.Window_TitlePrefix;
                string title = this.activeWorkspace?.Title?.Trim();

                if (!string.IsNullOrEmpty(title))
                {
                    return string.Format(CultureInfo.CurrentCulture, Resources.Window_TitleFormat, intro, title);
                }

                return intro;
            }
        }

        public IEnumerable<Api.IWorkspaceHolder> Workspaces => this.workspaces;
        public bool HasActiveWorkspace => this.ActiveWorkspace != null;
        public Api.ITabWorkspace ActiveTabWorkspace => this.ActiveWorkspace?.Workspace as Api.ITabWorkspace;
        public void Focus() => this.Window.Focus();

        public Api.IWorkspaceHolder FindWorkspace(Guid id)
        {
            return this.Workspaces.FirstOrDefault(w => w.Id == id);
        }

        public Api.IWorkspaceHolder ActiveWorkspace
        {
            get => this.activeWorkspace;

            set
            {
                IWorkspaceVM oldWorkspace = this.activeWorkspace;
                if (this.SetPropertyValue(ref this.activeWorkspace, value as IWorkspaceVM))
                {
                    if (this.activeWorkspace != null)
                    {
                        if (!this.workspaces.Contains(this.activeWorkspace))
                        {
                            this.AddWorkspace(this.activeWorkspace, activate: false);
                        }

                        this.activeWorkspace.ActiveState = Api.ActiveState.Active;
                    }

                    if (oldWorkspace != null)
                    {
                        oldWorkspace.ActiveState = Api.ActiveState.Hidden;
                    }

                    this.Window.ViewElement = this.activeWorkspace?.ViewElement;

                    this.OnPropertyChanged(nameof(this.HasActiveWorkspace));
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }

                this.FocusActiveWorkspace();
            }
        }

        public void FocusActiveWorkspace()
        {
            this.activeWorkspace?.Focus();
        }

        public Api.IWorkspaceHolder AddWorkspace(Api.IWorkspace workspace, bool activate)
        {
            WorkspaceVM workspaceVM = new WorkspaceVM(this, workspace);
            this.AddWorkspace(workspaceVM, activate);
            return workspaceVM;
        }

        public Api.IWorkspaceHolder AddWorkspace(Api.IWorkspaceProvider provider, bool activate)
        {
            WorkspaceVM workspaceVM = new WorkspaceVM(this, provider);
            this.AddWorkspace(workspaceVM, activate);
            return workspaceVM;
        }

        public Api.IWorkspaceHolder AddWorkspace(Api.IWorkspaceProvider provider, Api.IWorkspaceSnapshot snapshot, bool activate)
        {
            WorkspaceVM workspaceVM = new WorkspaceVM(this, provider, snapshot);
            this.AddWorkspace(workspaceVM, activate);
            return workspaceVM;
        }

        private void AddWorkspace(IWorkspaceVM workspace, bool activate)
        {
            if (!this.workspaces.Contains(workspace))
            {
                int index = this.workspaces.Count;
                this.workspaces.Insert(index, workspace);
                this.AddMainMenuItems(workspace);

                workspace.PropertyChanged += this.OnWorkspacePropertyChanged;
            }

            if (activate)
            {
                this.ActiveWorkspace = workspace;
            }
        }

        public void RemoveWorkspace(Api.IWorkspaceHolder workspace)
        {
            bool removingActive = (this.ActiveWorkspace == workspace);

            if (workspace is IWorkspaceVM workspaceVM && this.workspaces.Remove(workspaceVM))
            {
                if (removingActive)
                {
                    this.ActiveWorkspace = this.workspaces.FirstOrDefault();
                }

                this.RemoveMainMenuItems(workspaceVM);
                workspaceVM.PropertyChanged -= this.OnWorkspacePropertyChanged;
                workspaceVM.Dispose();
            }
        }

        private object ConvertWorkspaceMenuItemVisibility(object[] values, Type targetType, object parameter)
        {
            IWorkspaceVM workspace = (IWorkspaceVM)parameter;

            foreach (object value in values)
            {
                if (value is Visibility visibility)
                {
                    if (visibility != Visibility.Visible)
                    {
                        return Visibility.Collapsed;
                    }
                }
                else if (value == null || value is IWorkspaceVM)
                {
                    if (workspace != value)
                    {
                        return Visibility.Collapsed;
                    }
                }
            }

            return Visibility.Visible;
        }

        private void AddMainMenuItems(IWorkspaceVM workspace)
        {
            if (!this.workspaceMenuItems.ContainsKey(workspace) && workspace.CreatedWorkspace)
            {
                MenuItem[] items = workspace.MenuItems?.ToArray() ?? Array.Empty<MenuItem>();
                this.workspaceMenuItems[workspace] = items;

                int index = 1;
                foreach (MenuItem item in items)
                {
                    // Create a {Binding} so that the menu item is only visible when the workspace is active.
                    // But try and keep any existing {Binding} that's already set on the menu item.

                    MultiBinding binding = new MultiBinding();
                    binding.Mode = BindingMode.OneWay;
                    binding.Converter = this.workspaceMenuItemVisibilityConverter;
                    binding.ConverterParameter = workspace;

                    if (BindingOperations.GetBindingBase(item, UIElement.VisibilityProperty) is BindingBase visibilityBinding)
                    {
                        binding.Bindings.Add(visibilityBinding);
                    }

                    binding.Bindings.Add(new Binding(nameof(this.ActiveWorkspace))
                    {
                        Mode = BindingMode.OneWay,
                        Source = this,
                    });

                    item.DataContext = workspace.Workspace;
                    BindingOperations.SetBinding(item, UIElement.VisibilityProperty, binding);

                    this.Window.mainMenu.Items.Insert(index++, item);
                }
            }
        }

        private void RemoveMainMenuItems(IWorkspaceVM workspace)
        {
            if (this.workspaceMenuItems.TryGetValue(workspace, out MenuItem[] items))
            {
                this.workspaceMenuItems.Remove(workspace);

                foreach (MenuItem item in items)
                {
                    this.Window.mainMenu.Items.Remove(item);
                }

            }
        }

        private void OnWorkspacePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender is IWorkspaceVM workspace && this.ActiveWorkspace == workspace)
            {
                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspaceVM.ViewElement))
                {
                    this.Window.ViewElement = workspace.ViewElement;
                }

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspaceVM.Title))
                {
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspaceVM.MenuItems))
                {
                    this.RemoveMainMenuItems(workspace);
                    this.AddMainMenuItems(workspace);
                }
            }
        }

        public void InitWorkspaces(AppSnapshot snapshot)
        {
            foreach (Api.IWorkspaceProvider provider in this.Window.App.PluginState.WorkspaceProviders)
            {
                IWorkspaceVM workspace = new WorkspaceVM(this, provider, snapshot.FindWorkspaceSnapshot(provider.WorkspaceId));
                this.AddWorkspace(workspace, false);

                if (this.ActiveWorkspace == null || workspace.Id == snapshot.ActiveWorkspaceId)
                {
                    this.ActiveWorkspace = workspace;
                }
            }
        }

        private void StartConsole(ConsoleSettings settings)
        {
            if (this.FindWorkspace(Api.Constants.ProcessWorkspaceId) is IWorkspaceVM workspaceVM && workspaceVM.Workspace is Api.IProcessWorkspace workspace)
            {
                this.ActiveWorkspace = workspaceVM;
                workspace.RunProcess(settings);
            }
        }

        private void QuickStartConsole(int arg)
        {
            if (this.AppSettings.Consoles.ElementAtOrDefault(arg) is ConsoleSettings settings)
            {
                this.StartConsole(settings);
            }
        }

        private void StartLink(LinkSettings settings)
        {
            this.RunExternalProcess(settings.Address);
        }

        private void StartTool(ToolSettings settings)
        {
            this.RunExternalProcess(settings.ExpandedCommand, settings.ExpandedArguments);
        }

        private void StartVisualStudio(VisualStudioSetup.Instance instance)
        {
            this.RunExternalProcess(instance.ProductPath);
        }

        public void RunExternalProcess(string path, string arguments = null)
        {
            this.Window.infoBar.Clear();

            try
            {
                if (string.IsNullOrEmpty(arguments))
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Diagnostics.Process.Start(path, arguments);
                }
            }
            catch (Exception ex)
            {
                this.Window.infoBar.SetError(ex, string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToStart, path));
            }
        }
    }
}
