using DevPrompt.Settings;
using DevPrompt.UI.Controls;
using DevPrompt.UI.Settings;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the main window (handles all menu items, etc)
    /// </summary>
    internal sealed class MainWindowVM : Api.Utility.PropertyNotifier, Api.IWindow
    {
        public MainWindow Window { get; }
        public App App => this.Window.App;
        public AppSettings AppSettings => this.App.Settings;
        public AppUpdate AppUpdate => this.App.AppUpdate;
        public InfoBar InfoBar => this.Window.infoBar;
        public IReadOnlyList<ConsoleSettings> VisualStudioConsoles => this.visualStudioConsoles;
        Api.IApp Api.IWindow.App => this.App;
        IntPtr Api.IWindow.Handle => this.Window.Handle;
        Window Api.IWindow.Window => this.Window;
        Api.IProgressBar Api.IWindow.ProgressBar => this.Window.progressBar;
        Api.IInfoBar Api.IWindow.InfoBar => this.InfoBar;

        public ICommand ConsoleCommand { get; }
        public ICommand GrabConsoleCommand { get; }
        public ICommand LinkCommand { get; }
        public ICommand ToolCommand { get; }
        public ICommand VisualStudioCommand { get; }
        public ICommand QuickStartConsoleCommand { get; }

        private readonly ObservableCollection<ConsoleSettings> visualStudioConsoles;
        private readonly ObservableCollection<IWorkspaceVM> workspaces;
        private readonly Dictionary<IWorkspaceVM, FrameworkElement[]> workspaceMenuItems;
        private readonly Dictionary<IWorkspaceVM, KeyBinding[]> workspaceKeyBindings;
        private readonly IMultiValueConverter workspaceMenuItemVisibilityConverter;
        private IWorkspaceVM activeWorkspace;

        public MainWindowVM(MainWindow window)
        {
            this.Window = window;
            this.Window.Closed += this.OnWindowClosed;
            this.Window.Activated += this.OnWindowActivated;
            this.Window.Deactivated += this.OnWindowDeactivated;

            this.ConsoleCommand = new Api.Utility.DelegateCommand((object arg) => this.StartConsole((ConsoleSettings)arg));
            this.GrabConsoleCommand = new Api.Utility.DelegateCommand((object arg) => this.App.GrabProcess((int)arg));
            this.LinkCommand = new Api.Utility.DelegateCommand((object arg) => this.StartLink((LinkSettings)arg));
            this.ToolCommand = new Api.Utility.DelegateCommand((object arg) => this.StartTool((ToolSettings)arg));
            this.QuickStartConsoleCommand = new Api.Utility.DelegateCommand(async (object arg) => await this.QuickStartConsole((int)arg));

            this.visualStudioConsoles = new ObservableCollection<ConsoleSettings>();
            this.workspaces = new ObservableCollection<IWorkspaceVM>();
            this.workspaceMenuItems = new Dictionary<IWorkspaceVM, FrameworkElement[]>();
            this.workspaceKeyBindings = new Dictionary<IWorkspaceVM, KeyBinding[]>();
            this.workspaceMenuItemVisibilityConverter = new Api.Utility.DelegateMultiValueConverter(this.ConvertWorkspaceMenuItemVisibility);
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
            this.ActiveWorkspace?.Workspace?.OnWindowActivated();
        }

        private void OnWindowDeactivated(object sender, EventArgs args)
        {
            this.ActiveWorkspace?.Workspace?.OnWindowDeactivated();
        }

        public async Task<bool> UpdateVisualStudioConsolesAsync()
        {
            List<ConsoleSettings> consoles = new List<ConsoleSettings>(this.visualStudioConsoles.Count);
            if (this.AppSettings.ShowVisualStudioPrompts)
            {
                consoles.AddRange(await AppSettings.GetVisualStudioConsolesAsync());
            }

            if (this.visualStudioConsoles.SequenceEqual(consoles))
            {
                return false;
            }

            this.visualStudioConsoles.Clear();

            foreach (ConsoleSettings console in consoles)
            {
                this.visualStudioConsoles.Add(console);
            }

            return true;
        }

        public ICommand ExitCommand => new Api.Utility.DelegateCommand(() => this.Window.Close());

        public void ShowSettingsDialog(Api.SettingsTabType tab)
        {
            this.App.Telemetry.TrackEvent("Command.Settings", new Dictionary<string, object>()
            {
                { "Tab", tab },
            });

            using (SettingsDialog dialog = new SettingsDialog(this.Window, this.AppSettings, tab))
            {
                if (dialog.ShowDialog() == true)
                {
                    dialog.ViewModel.Settings.HasDefaultThemeKeys = false; // this will become true if the colors actually match the default
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

        public ICommand SettingsCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.ShowSettingsDialog(Api.SettingsTabType.Default);
        });

        public ICommand PluginsCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.ShowSettingsDialog(Api.SettingsTabType.Plugins);
        });

        public ICommand CustomizeConsoleGrabCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.ShowSettingsDialog(Api.SettingsTabType.Grab);
        });

        public ICommand CustomizeToolsCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.ShowSettingsDialog(Api.SettingsTabType.Tools);
        });

        public ICommand CustomizeLinksCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.ShowSettingsDialog(Api.SettingsTabType.Links);
        });

        public ICommand TelemetryCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.ShowSettingsDialog(Api.SettingsTabType.Telemetry);
        });

        public ICommand ReportAnIssueCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.App.Telemetry.TrackEvent("Command.ReportAnIssue");
            this.RunExternalProcess(Resources.About_IssuesLink);
        });

        public ICommand CheckForUpdatesCommand => new Api.Utility.DelegateCommand(async () =>
        {
            this.App.Telemetry.TrackEvent("Command.CheckForUpdates");
            await this.AppUpdate.CheckUpdateVersionAsync();
            this.InfoBar.SetInfo(Api.InfoErrorLevel.Message, (this.AppUpdate.State == Api.AppUpdateState.HasUpdate) ? Resources.AppUpdate_UpdateAvailable : Resources.AppUpdate_NoUpdateAvailable);
        });

        public ICommand AboutCommand => new Api.Utility.DelegateCommand(() =>
        {
            this.App.Telemetry.TrackEvent("Command.About");

            AboutDialog dialog = new AboutDialog(this)
            {
                Owner = this.Window
            };

            dialog.ShowDialog();
        });

        public ICommand DownloadUpdateCommand => new Api.Utility.DelegateCommand(async typeObject =>
        {
            if (typeObject is string type)
            {
                this.App.Telemetry.TrackEvent("Command.DownloadUpdate", new Dictionary<string, object>()
                {
                    { "Type", type },
                });

                try
                {
                    await this.AppUpdate.DownloadUpdate(this.Window, type);
                }
                catch (Exception ex)
                {
                    this.InfoBar.SetError(ex);
                }
            }
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
                this.AddKeyBindings(workspace);

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

                this.RemoveKeyBindings(workspaceVM);
                this.RemoveMainMenuItems(workspaceVM);
                workspaceVM.PropertyChanged -= this.OnWorkspacePropertyChanged;
                workspaceVM.Dispose();
            }
        }

        private object ConvertWorkspaceMenuItemVisibility(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                FrameworkElement[] items = workspace.MenuItems?.ToArray() ?? Array.Empty<FrameworkElement>();
                this.workspaceMenuItems[workspace] = items;

                int index = 1;
                foreach (MenuItem item in items)
                {
                    // Create a {Binding} so that the menu item is only visible when the workspace is active.
                    // But try and keep any existing {Binding} that's already set on the menu item.

                    MultiBinding binding = new MultiBinding
                    {
                        Mode = BindingMode.OneWay,
                        Converter = this.workspaceMenuItemVisibilityConverter,
                        ConverterParameter = workspace
                    };

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
            if (this.workspaceMenuItems.TryGetValue(workspace, out FrameworkElement[] items))
            {
                this.workspaceMenuItems.Remove(workspace);

                foreach (MenuItem item in items)
                {
                    this.Window.mainMenu.Items.Remove(item);
                }
            }
        }

        private KeyBinding WrapKeyBinding(IWorkspaceVM workspace, KeyBinding binding)
        {
            if (binding.Command == null)
            {
                return binding;
            }

            return new KeyBinding(new WorkspaceCommandWrapper(workspace, binding.Command), binding.Key, binding.Modifiers)
            {
                CommandParameter = binding.CommandParameter,
                CommandTarget = binding.CommandTarget,
            };
        }

        private void AddKeyBindings(IWorkspaceVM workspace)
        {
            if (!this.workspaceKeyBindings.ContainsKey(workspace) && workspace.CreatedWorkspace)
            {
                KeyBinding[] items = workspace.KeyBindings?.Select(b => this.WrapKeyBinding(workspace, b)).ToArray() ?? Array.Empty<KeyBinding>();
                this.workspaceKeyBindings[workspace] = items;

                foreach (KeyBinding item in items)
                {
                    this.Window.InputBindings.Add(item);
                }
            }
        }

        private void RemoveKeyBindings(IWorkspaceVM workspace)
        {
            if (this.workspaceKeyBindings.TryGetValue(workspace, out KeyBinding[] items))
            {
                this.workspaceKeyBindings.Remove(workspace);

                foreach (KeyBinding item in items)
                {
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    this.Window.InputBindings.Remove(item);
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

                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspaceVM.KeyBindings))
                {
                    this.RemoveKeyBindings(workspace);
                    this.AddKeyBindings(workspace);
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
                this.App.Telemetry.TrackEvent("Start.Console", new Dictionary<string, object>()
                {
                    { "ConsoleType", settings.ConsoleType },
                    { "TabCount", workspace.Tabs.Count() + 1 },
                });

                this.ActiveWorkspace = workspaceVM;
                workspace.RunProcess(settings);
            }
        }

        private async Task QuickStartConsole(int arg)
        {
            if (this.AppSettings.Consoles.ElementAtOrDefault(arg) is ConsoleSettings settings)
            {
                this.StartConsole(settings);
            }
            else if (arg >= this.AppSettings.Consoles.Count)
            {
                int i = arg - this.AppSettings.Consoles.Count;

                if (i >= this.visualStudioConsoles.Count)
                {
                    // Maybe a new VS was just installed
                    await this.UpdateVisualStudioConsolesAsync();
                }

                if (this.visualStudioConsoles.ElementAtOrDefault(i) is ConsoleSettings settings2)
                {
                    this.StartConsole(settings2);
                }
            }
        }

        private void StartLink(LinkSettings settings)
        {
            this.App.Telemetry.TrackEvent("Start.Link");
            this.RunExternalBrowser(settings.Browser, settings.Address);
        }

        private void StartTool(ToolSettings settings)
        {
            this.App.Telemetry.TrackEvent("Start.Tool");
            this.RunExternalProcess(settings.ExpandedCommand, settings.ExpandedArguments);
        }

        public void RunExternalProcess(string path, string arguments = null)
        {
            this.InfoBar.Clear();

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
                this.InfoBar.SetError(ex, string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToStart, path));
            }
        }

        public void RunExternalBrowser(string browserId, string url)
        {
            this.InfoBar.Clear();
            BrowserUtility.Browse(browserId, url, this);
        }
    }
}
