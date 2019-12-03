using DevPrompt.Interop;
using DevPrompt.Plugins;
using DevPrompt.Settings;
using DevPrompt.UI;
using DevPrompt.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    internal sealed partial class App : Application, IAppHost, Api.IApp, Api.IAppProcesses
    {
        public AppSettings Settings { get; }
        public HttpClientHelper HttpClient { get; }
        public Telemetry Telemetry { get; private set; }
        public VisualStudioSetup VisualStudioSetup { get; private set; }
        public PluginState PluginState { get; private set; }
        public AppUpdate AppUpdate { get; private set; }
        public NativeApp NativeApp { get; private set; }

        private enum RunningState { Run, RestartWindow, RestartApp, ShutDown }
        private enum SettingsState { None, Loaded, SavePending }

        private readonly List<Task> criticalTasks; // process won't exit until all critical tasks are done
        private readonly ConcurrentDictionary<string, Assembly> resolvedAssemblies;
        private RunningState runningState;
        private SettingsState settingsState;
        private SettingsState customSettingsState;

        public App()
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            this.criticalTasks = new List<Task>();
            this.resolvedAssemblies = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            this.Settings = new AppSettings();
            this.HttpClient = new HttpClientHelper();
            this.PluginState = new PluginState(this);
            this.AppUpdate = new AppUpdate(this);

            this.Startup += this.OnStartup;
            this.Exit += this.OnExit;

            SystemEvents.SessionEnded += this.OnSessionEnded;
            AppDomain.CurrentDomain.AssemblyResolve += this.OnFailedAssemblyResolve;
        }

        private void Dispose()
        {
            this.Settings.PropertyChanged -= this.OnSettingsPropertyChanged;
            this.Settings.CollectionChanged -= this.OnSettingsPropertyChanged;

            this.AppUpdate.Dispose();
            this.PluginState.Dispose();
            this.HttpClient.Dispose();
            this.NativeApp?.Dispose();
            this.Telemetry?.Dispose();
        }

        public new MainWindow MainWindow
        {
            get => base.MainWindow as MainWindow;
            set => base.MainWindow = value;
        }

        private async void OnStartup(object sender, StartupEventArgs args)
        {
            Stopwatch timer = Stopwatch.StartNew();
            string errorMessage = string.Empty;
            bool firstStartup = (this.NativeApp == null);

            this.Telemetry = await Telemetry.Create(this);
            this.VisualStudioSetup = new VisualStudioSetup();
            this.NativeApp = this.NativeApp ?? NativeMethods.CreateApp(this, out errorMessage);
            this.MainWindow = new MainWindow(this);
            this.MainWindow.infoBar.SetError(null, errorMessage);
            this.MainWindow.Show();
            TimeSpan createWindowTime = timer.GetElapsedTimeAndRestart();

            await this.InitSettings();
            this.settingsState = SettingsState.Loaded;
            TimeSpan settingsLoadTime = timer.GetElapsedTimeAndRestart();

            await this.PluginState.Initialize(firstStartup);
            TimeSpan pluginLoadTime = timer.GetElapsedTimeAndRestart();

            await this.InitCustomSettings();
            this.customSettingsState = SettingsState.Loaded;

            foreach (Api.IAppListener listener in this.PluginState.AppListeners)
            {
                listener.OnStartup(this);
            }

            AppSnapshot snapshot = await AppSnapshot.Load(this, AppSnapshot.DefaultSnapshotFile);
            this.MainWindow?.InitWorkspaces(snapshot);

            foreach (Api.IAppListener listener in this.PluginState.AppListeners)
            {
                if (this.MainWindow?.ViewModel is Api.IWindow window)
                {
                    listener.OnOpened(this, window);
                }
            }

            this.AppUpdate.Start();

            TimeSpan pluginInitTime = timer.GetElapsedTimeAndStop();

            this.Telemetry.TrackEvent("App.Startup", new Dictionary<string, object>()
            {
                { "CreateWindowTime", createWindowTime },
                { "Culture", Thread.CurrentThread.CurrentCulture.Name },
                { "DotNetVersion", Environment.Version },
                { "FirstStartup", firstStartup },
                { "FirstStartupTime", firstStartup ? (object)Program.TimeSinceStart : null },
                { "HasNativeApp", this.NativeApp != null },
                { "Is64BitProcess", Environment.Is64BitProcess },
                { "Is64BitOS ", Environment.Is64BitOperatingSystem },
                { "IsElevated", Program.IsElevated },
                { "IsMainProcess", Program.IsMainProcess },
                { "IsMicrosoftDomain", Program.IsMicrosoftDomain },
                { "OsPlatform", Environment.OSVersion.Platform },
                { "OsVersion", Environment.OSVersion.Version },
                { "PluginCount", this.PluginState.Plugins.Count() },
                { "PluginInitTime", pluginInitTime },
                { "PluginLoadTime", pluginLoadTime },
                { "SettingsLoadTime", settingsLoadTime },
                { "UICulture", Thread.CurrentThread.CurrentUICulture.Name },
                { "Version", Program.Version },
            });
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            Debug.Assert(this.MainWindow == null && this.criticalTasks.Count == 0);

            foreach (Api.IAppListener listener in this.PluginState.AppListeners)
            {
                listener.OnExit(this);
            }

            this.Dispose();
        }

        public void OnWindowClosing(MainWindow window)
        {
            foreach (Api.IAppListener listener in this.PluginState.AppListeners)
            {
                listener.OnClosing(this, window.ViewModel);
            }
        }

        public void OnWindowClosed(bool restartWindow, bool restartApp)
        {
            if (this.runningState == RunningState.Run)
            {
                if (restartApp)
                {
                    this.runningState = RunningState.RestartApp;
                }
                else if (restartWindow)
                {
                    this.runningState = RunningState.RestartWindow;
                }
            }

            this.CheckShutdown(windowClosed: true);
        }

        private async Task InitSettings()
        {
            if (this.settingsState == SettingsState.None)
            {
                AppSettings settings = await AppSettings.Load(this, AppSettings.SettingsFile);
                this.Settings.CopyFrom(settings);

                this.Settings.PropertyChanged += this.OnSettingsPropertyChanged;
                this.Settings.CollectionChanged += this.OnSettingsPropertyChanged;
            }
        }

        private async Task InitCustomSettings()
        {
            if (this.customSettingsState == SettingsState.None)
            {
                AppCustomSettings customSettings = await AppSettings.LoadCustom(this, AppSettings.CustomSettingsFile);
                this.Settings.CopyFrom(customSettings);
            }
        }

        private void OnSettingsPropertyChanged(object sender, EventArgs args)
        {
            this.SaveSettings();
        }

        public void SaveSettings(string path = null)
        {
            if (this.settingsState == SettingsState.Loaded)
            {
                this.settingsState = SettingsState.SavePending;

                Action action = () =>
                {
                    this.settingsState = SettingsState.Loaded;
                    this.Settings.Save(this, path);
                };

                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }
        }

        public void AddCriticalTask(Task task)
        {
            bool added = false;

            lock (this.criticalTasks)
            {
                if (this.runningState != RunningState.ShutDown && task != null && !this.criticalTasks.Contains(task))
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
                if (this.runningState != RunningState.ShutDown && (windowClosed || this.MainWindow == null))
                {
                    int taskCount = 0;
                    lock (this.criticalTasks)
                    {
                        taskCount = this.criticalTasks.Count;
                        if (taskCount == 0 && this.runningState == RunningState.Run)
                        {
                            this.runningState = RunningState.ShutDown;
                        }
                    }

                    if (taskCount == 0)
                    {
                        if (this.runningState == RunningState.ShutDown)
                        {
                            this.Shutdown();
                        }
                        else if (this.runningState == RunningState.RestartWindow)
                        {
                            this.RestartWindow();
                        }
                        else if (this.runningState == RunningState.RestartApp)
                        {
                            this.RestartApp();
                        }
                    }
                }
            };

            this.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }

        private void RestartWindow()
        {
            this.runningState = RunningState.Run;

            this.PluginState.Dispose();
            this.PluginState = new PluginState(this);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            this.OnStartup(this, null);
        }

        private void RestartApp()
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                Process process = Process.Start(new ProcessStartInfo(currentProcess.MainModule.FileName, $"/waitfor {currentProcess.Id}")
                {
                    UseShellExecute = true
                });

                if (process != null)
                {
                    this.runningState = RunningState.ShutDown;
                    this.Shutdown();
                    return;
                }
            }
            catch
            {
                // Failed to create a new process, so just restart the window instead
            }

            this.RestartWindow();
        }

        private void OnSessionEnded(object sender, SessionEndedEventArgs args)
        {
            this.MainWindow?.OnSessionEnded();
        }

        private void CallProcessListeners(Action<IProcessListener> action)
        {
            foreach (IProcessListener listener in this.PluginState.ProcessListeners)
            {
                action(listener);
            }
        }

        void IAppHost.TrackEvent(string eventName)
        {
            this.Telemetry.TrackEvent(eventName);
        }

        void IAppHost.OnProcessOpening(IProcess process, bool activate, string path)
        {
            this.CallProcessListeners(l => l.OnProcessOpening(process, activate, path));
        }

        void IAppHost.OnProcessClosing(IProcess process)
        {
            this.CallProcessListeners(l => l.OnProcessClosing(process));
        }

        void IAppHost.OnProcessEnvChanged(IProcess process, string env)
        {
            this.CallProcessListeners(l => l.OnProcessEnvChanged(process, env));
        }

        void IAppHost.OnProcessTitleChanged(IProcess process, string title)
        {
            this.CallProcessListeners(l => l.OnProcessTitleChanged(process, title));
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
        Api.IAppUpdate Api.IApp.AppUpdate => this.AppUpdate;
        Api.IAppProcesses Api.IApp.AppProcesses => this;
        Api.ITelemetry Api.IApp.Telemetry => this.Telemetry;
        Api.IVisualStudioSetup Api.IApp.VisualStudioSetup => this.VisualStudioSetup;
        Api.IWindow Api.IApp.ActiveWindow => this.MainWindow?.ViewModel;
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

        IEnumerable<Api.GrabProcess> Api.IAppProcesses.GrabProcesses
        {
            get
            {
                string names = this.NativeApp?.GrabProcesses ?? string.Empty;
                return names.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(n => new Api.GrabProcess(n));
            }
        }

        // Api.IAppProcesses
        public void GrabProcess(int id)
        {
            this.Telemetry.TrackEvent("Grab.ManualGrabProcess");
            this.NativeApp?.GrabProcess(id);
        }

        Api.IProcessHost Api.IAppProcesses.CreateProcessHost(IntPtr parentHwnd)
        {
            if (this.PluginState.ProcessCache != null)
            {
                return this.NativeApp?.CreateProcessHost(this.PluginState.ProcessCache, parentHwnd);
            }

            Debug.Fail("CreateProcessHost called before app init is complete");
            return null;
        }

        void Api.IAppProcesses.RunExternalProcess(string path, string arguments)
        {
            this.MainWindow?.ViewModel.RunExternalProcess(path, arguments);
        }

        private Assembly OnFailedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (this.resolvedAssemblies.TryGetValue(args.Name, out Assembly resolvedAssembly))
                {
                    return resolvedAssembly;
                }

                // Maybe an exact match was already loaded
                foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (loadedAssembly.FullName.Equals(args.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        this.resolvedAssemblies.TryAdd(args.Name, loadedAssembly);
                        return loadedAssembly;
                    }
                }

                // Search plugins for an possible match
                IEnumerable<InstalledPluginInfo> infos = this.PluginState.Plugins.Select(p => p.PluginInfo);
                InstalledPluginAssemblyInfo match = InstalledPluginInfo.FindBestAssemblyMatch(new AssemblyName(args.Name), infos);

                if (match != null && File.Exists(match.Path))
                {
                    Assembly bestAssembly = Assembly.LoadFrom(match.Path);
                    this.resolvedAssemblies.TryAdd(args.Name, bestAssembly);
                    return bestAssembly;
                }
            }
            catch
            {
                Debug.Fail($"Failed resolving assembly: {args.Name}");
            }

            this.resolvedAssemblies.TryAdd(args.Name, null);
            return null;
        }
    }
}
