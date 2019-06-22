using DevPrompt.Settings;
using DevPrompt.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the main window (handles all menu items, etc)
    /// </summary>
    internal class MainWindowVM : Api.PropertyNotifier, Api.IWindow
    {
        public MainWindow Window { get; }
        public App App => this.Window.App;
        public AppSettings AppSettings => this.App.Settings;
        Window Api.IWindow.Window => this.Window;
        Api.IApp Api.IWindow.App => this.App;

        public ICommand ConsoleCommand { get; }
        public ICommand GrabConsoleCommand { get; }
        public ICommand LinkCommand { get; }
        public ICommand ToolCommand { get; }
        public ICommand VisualStudioCommand { get; }

        private ObservableCollection<Api.IWorkspaceVM> workspaces;
        private Dictionary<Api.IWorkspaceVM, MenuItem[]> workspaceMenuItems;
        private IMultiValueConverter workspaceMenuItemVisibilityConverter;
        private Api.IWorkspaceVM activeWorkspace;
        private readonly Stack<Action> loadingCancelActions;
        private string errorText;

        public MainWindowVM(MainWindow window)
        {
            this.Window = window;
            this.Window.Closed += this.OnWindowClosed;
            this.Window.Activated += this.OnWindowActivated;
            this.Window.Deactivated += this.OnWindowDeactivated;

            this.ConsoleCommand = new Api.DelegateCommand((object arg) => this.StartConsole((ConsoleSettings)arg));
            this.GrabConsoleCommand = new Api.DelegateCommand((object arg) => this.App.GrabProcess((int)arg));
            this.LinkCommand = new Api.DelegateCommand((object arg) => this.StartLink((LinkSettings)arg));
            this.ToolCommand = new Api.DelegateCommand((object arg) => this.StartTool((ToolSettings)arg));
            this.VisualStudioCommand = new Api.DelegateCommand((object arg) => this.StartVisualStudio((VisualStudioSetup.Instance)arg));

            this.workspaces = new ObservableCollection<Api.IWorkspaceVM>();
            this.workspaceMenuItems = new Dictionary<Api.IWorkspaceVM, MenuItem[]>();
            this.workspaceMenuItemVisibilityConverter = new DelegateMultiConverter(this.ConvertWorkspaceMenuItemVisibility);
            this.loadingCancelActions = new Stack<Action>();
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

        public ICommand ClearErrorTextCommand => new Api.DelegateCommand(this.ClearErrorText);

        public ICommand ExitCommand => new Api.DelegateCommand(() => this.Window.Close());

        public ICommand VisualStudioInstallerCommand => new Api.DelegateCommand(() =>
        {
            this.RunExternalProcess(VisualStudioSetup.InstallerPath);
        });

        public ICommand VisualStudioDogfoodCommand => new Api.DelegateCommand(() =>
        {
            this.RunExternalProcess(VisualStudioSetup.DogfoodInstallerPath);
        });

        public ICommand InstallVisualStudioBranchCommand => new Api.DelegateCommand(() =>
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

        public ICommand CustomizeConsolesCommand => new Api.DelegateCommand(() =>
        {
            CustomizeConsolesDialog dialog = new CustomizeConsolesDialog(this.AppSettings.Consoles, this.AppSettings.SaveTabsOnExit)
            {
                Owner = this.Window
            };

            if (dialog.ShowDialog() == true)
            {
                this.AppSettings.SaveTabsOnExit = dialog.SaveTabs;
                this.AppSettings.Consoles.Clear();

                foreach (ConsoleSettings settings in dialog.Settings)
                {
                    this.AppSettings.Consoles.Add(settings.Clone());
                }
            }
        });

        public ICommand SettingsImportCommand => new Api.DelegateCommand(async () =>
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

        public ICommand SettingsExportCommand => new Api.DelegateCommand(() =>
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
                this.App.SaveSettings(dialog.FileName);
            }
        });

        public ICommand CustomizeConsoleGrabCommand => new Api.DelegateCommand(() =>
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

        public ICommand CustomizeToolsCommand => new Api.DelegateCommand(() =>
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

        public ICommand CustomizeLinksCommand => new Api.DelegateCommand(() =>
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

        public ICommand AboutCommand => new Api.DelegateCommand(() =>
        {
            AboutDialog dialog = new AboutDialog(this)
            {
                Owner = this.Window
            };

            dialog.ShowDialog();
        });

        public ICommand SetActiveTabNameCommand => new Api.DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.ActiveTab?.SetTabNameCommand?.SafeExecute();
        });

        public ICommand CloseActiveTabCommand => new Api.DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.ActiveTab?.CloseCommand?.SafeExecute();
        });

        public ICommand DetachActiveTabCommand => new Api.DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.ActiveTab?.DetachCommand?.SafeExecute();
        });

        public ICommand CloneActiveTabCommand => new Api.DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.ActiveTab?.CloneCommand?.SafeExecute();
        });

        public ICommand TabCycleNextCommand => new Api.DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.TabCycleNext();
        });

        public ICommand TabCyclePrevCommand => new Api.DelegateCommand(() =>
        {
            this.ActiveTabWorkspace?.TabCyclePrev();
        });

        public string WindowTitle
        {
            get
            {
                string intro = Program.IsElevated ? "[Dev Admin]" : "[Dev]";
                string title = this.ActiveWorkspace?.Title?.Trim();

                if (!string.IsNullOrEmpty(title))
                {
                    return $"{intro} {title}";
                }

                return intro;
            }
        }

        public IEnumerable<Api.IWorkspaceVM> Workspaces => this.workspaces;
        public bool HasActiveWorkspace => this.ActiveWorkspace != null;
        public Api.ITabWorkspace ActiveTabWorkspace => this.ActiveWorkspace?.Workspace as Api.ITabWorkspace;

        public Api.IWorkspaceVM FindWorkspace(Guid id)
        {
            return this.Workspaces.FirstOrDefault(w => w.Id == id);
        }

        public Api.IWorkspaceVM ActiveWorkspace
        {
            get => this.activeWorkspace;

            set
            {
                Api.IWorkspaceVM oldWorkspace = this.ActiveWorkspace;
                if (this.SetPropertyValue(ref this.activeWorkspace, value))
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

                    this.Window.ViewElement = this.ActiveWorkspace?.ViewElement;

                    this.OnPropertyChanged(nameof(this.HasActiveWorkspace));
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }

                this.FocusActiveWorkspace();
            }
        }

        public void FocusActiveWorkspace()
        {
            this.ActiveWorkspace?.Workspace?.Focus();
        }

        public void AddWorkspace(Api.IWorkspaceVM workspace, bool activate)
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

        public void RemoveWorkspace(Api.IWorkspaceVM workspace)
        {
            bool removingActive = (this.ActiveWorkspace == workspace);

            if (this.workspaces.Remove(workspace))
            {
                if (removingActive)
                {
                    this.ActiveWorkspace = this.workspaces.FirstOrDefault();
                }

                this.RemoveMainMenuItems(workspace);
                workspace.PropertyChanged -= this.OnWorkspacePropertyChanged;
                workspace.Dispose();
            }
        }

        private object ConvertWorkspaceMenuItemVisibility(object[] values, Type targetType, object parameter)
        {
            Api.IWorkspaceVM workspace = (Api.IWorkspaceVM)parameter;

            foreach (object value in values)
            {
                if (value is Visibility visibility)
                {
                    if (visibility != Visibility.Visible)
                    {
                        return Visibility.Collapsed;
                    }
                }
                else if (value == null || value is Api.IWorkspaceVM)
                {
                    if (workspace != value)
                    {
                        return Visibility.Collapsed;
                    }
                }
            }

            return Visibility.Visible;
        }

        private void AddMainMenuItems(Api.IWorkspaceVM workspace)
        {
            if (!this.workspaceMenuItems.ContainsKey(workspace) && workspace.CreatedWorkspace)
            {
                MenuItem[] items = workspace.MenuItems.ToArray();
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

        private void RemoveMainMenuItems(Api.IWorkspaceVM workspace)
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
            if (sender is Api.IWorkspaceVM workspace && this.ActiveWorkspace == workspace)
            {
                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.IWorkspaceVM.ViewElement))
                {
                    this.Window.ViewElement = workspace.ViewElement;
                }

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.IWorkspaceVM.Title))
                {
                    this.OnPropertyChanged(nameof(this.WindowTitle));
                }

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.IWorkspaceVM.MenuItems))
                {
                    this.RemoveMainMenuItems(workspace);
                    this.AddMainMenuItems(workspace);
                }
            }
        }

        public void InitWorkspaces(AppSnapshot snapshot)
        {
            foreach (Api.IWorkspaceProvider provider in this.Window.App.WorkspaceProviders)
            {
                Api.IWorkspaceVM workspace = new Api.WorkspaceVM(this, provider, snapshot.FindWorkspaceSnapshot(provider.WorkspaceId));
                this.AddWorkspace(workspace, false);

                if (this.ActiveWorkspace == null || workspace.Id == snapshot.ActiveWorkspaceId)
                {
                    this.ActiveWorkspace = workspace;
                }
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

        public bool Loading
        {
            get
            {
                return this.loadingCancelActions.Count > 0;
            }
        }

        public IDisposable BeginLoading(Action cancelAction, string text)
        {
            this.loadingCancelActions.Push(cancelAction);

            if (this.loadingCancelActions.Count == 1)
            {
                this.OnPropertyChanged(nameof(this.Loading));
            }

            return new DelegateDisposable(() =>
            {
                this.loadingCancelActions.Pop();

                if (this.loadingCancelActions.Count == 0)
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

        private void ClearErrorText()
        {
            this.SetError(null);
        }

        private void StartConsole(ConsoleSettings settings)
        {
            if (this.FindWorkspace(Api.Constants.ProcessWorkspaceId) is Api.IWorkspaceVM workspaceVM && workspaceVM.Workspace is Api.IProcessWorkspace workspace)
            {
                this.ActiveWorkspace = workspaceVM;
                workspace.RunProcess(settings);
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
            this.ClearErrorText();

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
                this.SetError(ex, $"Error: Failed to start \"{path}\"");
            }
        }
    }
}
