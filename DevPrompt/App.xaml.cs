﻿using DevPrompt.Interop;
using DevPrompt.Plugins;
using DevPrompt.Settings;
using DevPrompt.UI;
using DevPrompt.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public HttpClientHelper HttpClient { get; }
        public PluginState PluginState { get; private set; }
        public NativeApp NativeApp { get; private set; }

        private enum RunningState { Run, Restart, ShutDown }
        private enum SettingsState { None, Loaded, SavePending }

        private List<Task> criticalTasks; // process won't exit until all critical tasks are done
        private RunningState runningState;
        private SettingsState settingsState;

        public App()
        {
            this.criticalTasks = new List<Task>();

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            this.Settings = new AppSettings();
            this.HttpClient = new HttpClientHelper();
            this.PluginState = new PluginState(this);

            this.Startup += this.OnStartup;
            this.Exit += this.OnExit;

            SystemEvents.SessionEnded += this.OnSessionEnded;
        }

        private void Dispose()
        {
            this.Settings.PropertyChanged -= this.OnSettingsPropertyChanged;
            this.Settings.CollectionChanged -= this.OnSettingsPropertyChanged;

            this.PluginState.Dispose();
            this.HttpClient.Dispose();
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
            await this.PluginState.Initialize();
            await this.InitCustomSettings();
            this.settingsState = SettingsState.Loaded;

            foreach (Api.IAppListener listener in this.PluginState.AppListeners)
            {
                listener.OnStartup(this);
            }

            AppSnapshot snapshot = await AppSnapshot.Load(this, AppSnapshot.DefaultPath);
            this.MainWindow?.InitWorkspaces(snapshot);

            foreach (Api.IAppListener listener in this.PluginState.AppListeners)
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

        public void OnWindowClosed(MainWindow window, bool restart)
        {
            if (this.runningState == RunningState.Run && restart)
            {
                this.runningState = RunningState.Restart;
            }

            this.CheckShutdown(windowClosed: true);
        }

        private async Task InitSettings()
        {
            if (this.settingsState == SettingsState.None)
            {
                AppSettings settings = await AppSettings.Load(this, AppSettings.DefaultPath);
                this.Settings.CopyFrom(settings);

                this.Settings.PropertyChanged += this.OnSettingsPropertyChanged;
                this.Settings.CollectionChanged += this.OnSettingsPropertyChanged;
            }
        }

        private async Task InitCustomSettings()
        {
            if (this.settingsState == SettingsState.None)
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
                        else if (this.runningState == RunningState.Restart)
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
            this.runningState = RunningState.Run;

            this.PluginState.Dispose();
            this.PluginState = new PluginState(this);


            GC.Collect();
            GC.WaitForPendingFinalizers();

            this.OnStartup(this, null);
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
            if (this.PluginState.ProcessCache != null)
            {
                return this.NativeApp?.CreateProcessHost(this.PluginState.ProcessCache, parentHwnd);
            }

            Debug.Fail("CreateProcessHost called before app init is complete");
            return null;
        }
    }
}
