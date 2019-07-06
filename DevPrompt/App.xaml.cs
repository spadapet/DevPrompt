using DevPrompt.Interop;
using DevPrompt.Plugins;
using DevPrompt.Settings;
using DevPrompt.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DevPrompt
{
    /// <summary>
    /// WPF app object.
    /// The native code will call into here using the IAppHost interface. We call into
    /// native code using the IApp interface.
    /// </summary>
    internal partial class App : Application, IAppHost, Api.IApp
    {
        public AppSettings Settings { get; }
        public NativeApp NativeApp { get; private set; }
        public bool ArePluginsInitialized => this.processCache != null;
        public IEnumerable<IProcessListener> ProcessListeners => this.processListeners ?? Enumerable.Empty<IProcessListener>();
        public IEnumerable<Api.IAppListener> AppListeners => this.appListeners ?? Enumerable.Empty<Api.IAppListener>();
        public IEnumerable<Api.IMenuItemProvider> MenuItemProviders => this.menuItemProviders ?? Enumerable.Empty<Api.IMenuItemProvider>();
        public IEnumerable<Api.IWorkspaceProvider> WorkspaceProviders => this.workspaceProviders ?? Enumerable.Empty<Api.IWorkspaceProvider>();
        public IEnumerable<Api.IPluginInfo> PluginInfos => this.pluginInfos ?? Enumerable.Empty<Api.IPluginInfo>();
        public IEnumerable<Assembly> PluginAssemblies => this.pluginAssemblies ?? Enumerable.Empty<Assembly>();

        private CompositionHost compositionHost;
        private IProcessCache processCache;
        private IProcessListener[] processListeners;
        private Api.IAppListener[] appListeners;
        private Api.IMenuItemProvider[] menuItemProviders;
        private Api.IWorkspaceProvider[] workspaceProviders;
        private Api.IPluginInfo[] pluginInfos;
        private List<Assembly> pluginAssemblies;
        private bool saveSettingsPending;
        private bool loadedSettings;
        private List<Task> criticalTasks;
        private State state;

        private enum State
        {
            Run,
            Restart,
            ShutDown,
        }

        public App()
        {
            this.criticalTasks = new List<Task>();

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            this.Settings = new AppSettings();
            this.Startup += this.OnStartup;
            this.Exit += this.OnExit;

            SystemEvents.SessionEnded += this.OnSessionEnded;
        }

        private void Dispose()
        {
            this.Settings.PropertyChanged -= this.OnSettingsPropertyChanged;
            this.Settings.ObservableConsoles.CollectionChanged -= this.OnSettingsPropertyChanged;
            this.Settings.ObservableGrabConsoles.CollectionChanged -= this.OnSettingsPropertyChanged;
            this.Settings.ObservableLinks.CollectionChanged -= this.OnSettingsPropertyChanged;
            this.Settings.ObservableTools.CollectionChanged -= this.OnSettingsPropertyChanged;

            this.ClearPlugins();
            this.NativeApp?.Dispose();
        }

        public new MainWindow MainWindow
        {
            get => base.MainWindow as MainWindow;
            set => base.MainWindow = value;
        }

        private async void OnStartup(object sender, StartupEventArgs args)
        {
            string errorMessage = string.Empty;
            this.NativeApp = this.NativeApp ?? NativeMethods.CreateApp(this, out errorMessage);
            this.MainWindow = new MainWindow(this, errorMessage);
            this.MainWindow.Show();

            await this.InitSettings();
            await this.InitPlugins();
            await this.InitCustomSettings();
            this.loadedSettings = true;
            AppSnapshot snapshot = await AppSnapshot.Load(this, AppSnapshot.DefaultPath);

            foreach (Api.IAppListener listener in this.AppListeners)
            {
                listener.OnStartup(this);
            }

            this.MainWindow?.InitWorkspaces(snapshot);

            foreach (Api.IAppListener listener in this.AppListeners)
            {
                if (this.MainWindow?.ViewModel is Api.IWindow window)
                {
                    listener.OnOpened(this, window);
                }
            }
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            Debug.Assert(this.MainWindow == null && this.criticalTasks.Count == 0);

            foreach (Api.IAppListener listener in this.AppListeners)
            {
                listener.OnExit(this);
            }

            this.Dispose();
        }

        public void OnWindowClosing(MainWindow window)
        {
            foreach (Api.IAppListener listener in this.AppListeners)
            {
                listener.OnClosing(this, window.ViewModel);
            }
        }

        public void OnWindowClosed(MainWindow window, bool restart)
        {
            if (this.state == State.Run && restart)
            {
                this.state = State.Restart;
            }

            this.CheckShutdown(windowClosed: true);
        }

        private async Task InitPlugins()
        {
            this.pluginAssemblies = new List<Assembly>();
            this.compositionHost = await PluginUtility.CreatePluginHost(this, this.pluginAssemblies);

            if (this.compositionHost != null)
            {
                this.processCache = this.compositionHost.GetExport<Interop.IProcessCache>();
                this.appListeners = this.GetOrderedExports<Api.IAppListener>().ToArray();
                this.processListeners = this.GetOrderedExports<Interop.IProcessListener>().ToArray();
                this.menuItemProviders = this.GetOrderedExports<Api.IMenuItemProvider>().ToArray();
                this.workspaceProviders = this.GetOrderedExports<Api.IWorkspaceProvider>().ToArray();
                this.pluginInfos = this.GetOrderedExports<Api.IPluginInfo>().ToArray();
            }
            else
            {
                // Plugin support is broken (maybe missing DLLs) but the app should work anyway
                this.processCache = new Interop.NativeProcessCache();
            }
        }

        private void ClearPlugins()
        {
            this.compositionHost?.Dispose();
            this.compositionHost = null;
            this.pluginAssemblies = null;

            this.processCache = null;
            this.appListeners = null;
            this.processListeners = null;
            this.menuItemProviders = null;
            this.workspaceProviders = null;
            this.pluginInfos = null;
        }

        private async Task InitSettings()
        {
            if (!this.loadedSettings)
            {
                AppSettings settings = await AppSettings.Load(this, AppSettings.DefaultPath);
                this.Settings.CopyFrom(settings);

                this.Settings.PropertyChanged += this.OnSettingsPropertyChanged;
                this.Settings.ObservableConsoles.CollectionChanged += this.OnSettingsPropertyChanged;
                this.Settings.ObservableGrabConsoles.CollectionChanged += this.OnSettingsPropertyChanged;
                this.Settings.ObservableLinks.CollectionChanged += this.OnSettingsPropertyChanged;
                this.Settings.ObservableTools.CollectionChanged += this.OnSettingsPropertyChanged;
                this.Settings.ObservablePluginDirectories.CollectionChanged += this.OnSettingsPropertyChanged;
            }
        }

        private async Task InitCustomSettings()
        {
            if (!this.loadedSettings)
            {
                AppCustomSettings customSettings = await AppSettings.LoadCustom(this, AppSettings.DefaultCustomPath);
                this.Settings.CopyFrom(customSettings);
            }
        }

        private void OnSettingsPropertyChanged(object sender, EventArgs args)
        {
            this.SaveSettings();
        }

        public void SaveSettings(string path = null)
        {
            if (this.loadedSettings && !this.saveSettingsPending)
            {
                this.saveSettingsPending = true;

                Action action = () =>
                {
                    this.saveSettingsPending = false;
                    this.Settings.Save(this, path);
                };

                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }
        }

        private IEnumerable<T> GetOrderedExports<T>()
        {
            return this.compositionHost.GetExports<ExportFactory<T, Api.OrderAttribute>>()
                .OrderBy(i => i.Metadata.Order)
                .Select(i => i.CreateExport().Value);
        }

        public void AddCriticalTask(Task task)
        {
            bool added = false;

            lock (this.criticalTasks)
            {
                if (this.state != State.ShutDown && task != null && !this.criticalTasks.Contains(task))
                {
                    this.criticalTasks.Add(task);
                    added = true;
                }
            }

            if (added)
            {
                task.ContinueWith(this.RemoveCriticalTask);
            }
        }

        private void RemoveCriticalTask(Task task)
        {
            bool done = false;

            lock (this.criticalTasks)
            {
                this.criticalTasks.Remove(task);
                done = (this.criticalTasks.Count == 0);
            }

            if (done)
            {
                this.CheckShutdown(windowClosed: false);
            }
        }

        private void CheckShutdown(bool windowClosed)
        {
            Action action = () =>
            {
                if (this.state != State.ShutDown && (windowClosed || this.MainWindow == null))
                {
                    int taskCount = 0;
                    lock (this.criticalTasks)
                    {
                        taskCount = this.criticalTasks.Count;
                        if (taskCount == 0 && this.state == State.Run)
                        {
                            this.state = State.ShutDown;
                        }
                    }

                    if (taskCount == 0)
                    {
                        if (this.state == State.ShutDown)
                        {
                            this.Shutdown();
                        }
                        else if (this.state == State.Restart)
                        {
                            this.Restart();
                        }
                    }
                }
            };

            this.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }

        private void Restart()
        {
            this.state = State.Run;
            this.ClearPlugins();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            this.OnStartup(this, null);
        }

        private void OnSessionEnded(object sender, SessionEndedEventArgs args)
        {
            this.MainWindow?.OnSessionEnded();
        }

        void IAppHost.OnProcessOpening(Interop.IProcess process, bool activate, string path)
        {
            foreach (Interop.IProcessListener listener in this.ProcessListeners)
            {
                listener.OnProcessOpening(process, activate, path);
            }
        }

        void IAppHost.OnProcessClosing(Interop.IProcess process)
        {
            foreach (Interop.IProcessListener listener in this.ProcessListeners)
            {
                listener.OnProcessClosing(process);
            }
        }

        void IAppHost.OnProcessEnvChanged(Interop.IProcess process, string env)
        {
            foreach (Interop.IProcessListener listener in this.ProcessListeners)
            {
                listener.OnProcessEnvChanged(process, env);
            }
        }

        void IAppHost.OnProcessTitleChanged(Interop.IProcess process, string title)
        {
            foreach (Interop.IProcessListener listener in this.ProcessListeners)
            {
                listener.OnProcessTitleChanged(process, title);
            }
        }

        int IAppHost.CanGrab(string exePath, bool automatic)
        {
            if (automatic && !Program.IsMainProcess)
            {
                // Only one process at a time can automatically grab consoles
                return 0;
            }

            if ((!automatic || this.Settings.ConsoleGrabEnabled) && !string.IsNullOrEmpty(exePath))
            {
                foreach (GrabConsoleSettings console in this.Settings.GrabConsoles)
                {
                    if (console.CanGrab(exePath))
                    {
                        return console.TabActivate ? 2 : 1;
                    }
                }
            }

            return 0;
        }

        Api.IAppSettings Api.IApp.Settings => this.Settings;
        Dispatcher Api.IApp.Dispatcher => this.Dispatcher;
        bool Api.IApp.IsElevated => Program.IsElevated;
        bool Api.IApp.IsMainProcess => Program.IsMainProcess;
        bool Api.IApp.IsMicrosoftDomain => Program.IsMicrosoftDomain;

        IEnumerable<Api.IWindow> Api.IApp.Windows
        {
            get
            {
                if (this.MainWindow?.ViewModel is Api.IWindow window)
                {
                    yield return window;
                }
            }
        }

        IEnumerable<Api.GrabProcess> Api.IApp.GrabProcesses
        {
            get
            {
                string names = this.NativeApp?.GrabProcesses ?? string.Empty;
                return names.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(n => new Api.GrabProcess(n));
            }
        }

        // Api.IApp
        public void GrabProcess(int id)
        {
            this.NativeApp?.GrabProcess(id);
        }

        Api.IProcessHost Api.IApp.CreateProcessHost(IntPtr parentHwnd)
        {
            if (this.processCache != null)
            {
                return this.NativeApp?.CreateProcessHost(this.processCache, parentHwnd);
            }

            Debug.Fail("CreateProcessHost called before app init is complete");
            return null;
        }
    }
}
