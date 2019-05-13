using DevPrompt.Interop;
using DevPrompt.Plugins;
using DevPrompt.Settings;
using DevPrompt.UI;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
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
        public IEnumerable<IProcessListener> ProcessListeners => this.processListeners ?? Enumerable.Empty<IProcessListener>();
        public IEnumerable<Api.IAppListener> AppListeners => this.appListeners ?? Enumerable.Empty<Api.IAppListener>();
        public IEnumerable<Api.IMenuItemProvider> MenuItemProviders => this.menuItemProviders ?? Enumerable.Empty<Api.IMenuItemProvider>();
        public IEnumerable<Api.IWorkspaceProvider> WorkspaceProviders => this.workspaceProviders ?? Enumerable.Empty<Api.IWorkspaceProvider>();
        public IEnumerable<Assembly> PluginAssemblies => this.pluginAssemblies ?? Enumerable.Empty<Assembly>();

        private CompositionHost compositionHost;
        private IProcessCache processCache;
        private IProcessListener[] processListeners;
        private Api.IAppListener[] appListeners;
        private Api.IMenuItemProvider[] menuItemProviders;
        private Api.IWorkspaceProvider[] workspaceProviders;
        private List<Assembly> pluginAssemblies;

        public App()
        {
            this.Settings = new AppSettings();
            this.Startup += this.OnStartup;
            this.Exit += this.OnExit;
        }

        private void Dispose()
        {
            this.compositionHost?.Dispose();
            this.NativeApp?.Dispose();
        }

        public new MainWindow MainWindow
        {
            get => base.MainWindow as MainWindow;
            set => base.MainWindow = value;
        }

        private Api.ITabWorkspace ActiveTabWorkspace => this.MainWindow?.ViewModel.ActiveWorkspace?.Workspace as Api.ITabWorkspace;
        private Api.ITabVM ActiveTab => this.ActiveTabWorkspace?.ActiveTab;

        private async void OnStartup(object sender, StartupEventArgs args)
        {
            this.NativeApp = NativeMethods.CreateApp(this, out string errorMessage);
            this.MainWindow = new MainWindow(this, errorMessage);
            this.MainWindow.Show();

            this.InitPlugins();
            this.Settings.CopyFrom(await AppSettings.Load(this, AppSettings.DefaultPath));
            this.MainWindow.ViewModel.InitWorkspaces(await AppSnapshot.Load(this, AppSnapshot.DefaultPath));

            foreach (Api.IAppListener listener in this.AppListeners)
            {
                listener.OnStartup(this);
            }
        }

        public void OnWindowClosing(MainWindow window)
        {
            foreach (Api.IAppListener listener in this.AppListeners)
            {
                listener.OnClosing(this, window.ViewModel);
            }
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            foreach (Api.IAppListener listener in this.AppListeners)
            {
                listener.OnExit(this);
            }

            this.Dispose();
        }

        private void InitPlugins()
        {
            this.pluginAssemblies = new List<Assembly>();
            this.compositionHost = PluginUtility.CreatePluginHost(this, this.pluginAssemblies);

            if (this.compositionHost != null)
            {
                this.processCache = this.compositionHost.GetExport<Interop.IProcessCache>();
                this.appListeners = this.compositionHost.GetExports<Api.IAppListener>().ToArray();
                this.processListeners = this.compositionHost.GetExports<Interop.IProcessListener>().ToArray();
                this.menuItemProviders = this.compositionHost.GetExports<Api.IMenuItemProvider>().ToArray();
                this.workspaceProviders = this.compositionHost.GetExports<Api.IWorkspaceProvider>().ToArray();
            }
            else
            {
                // Plugin support is broken (maybe missing DLLs) but the app should work anyway
                this.processCache = new Interop.NativeProcessCache();
            }
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

        void IAppHost.CloneActiveProcess()
        {
            this.ActiveTab?.CloneCommand?.SafeExecute();
        }

        void IAppHost.SetTabName()
        {
            this.ActiveTab?.SetTabNameCommand?.SafeExecute();
        }

        void IAppHost.CloseActiveProcess()
        {
            this.ActiveTab?.CloseCommand?.SafeExecute();
        }

        void IAppHost.DetachActiveProcess()
        {
            this.ActiveTab?.DetachCommand?.SafeExecute();
        }

        IntPtr IAppHost.GetMainWindow()
        {
            if (this.MainWindow is Window window)
            {
                WindowInteropHelper helper = new WindowInteropHelper(window);
                return helper.Handle;
            }

            return IntPtr.Zero;
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

        void IAppHost.OnSystemShutdown()
        {
            this.MainWindow?.OnSystemShutdown();
        }

        void IAppHost.OnAltLetter(int vk)
        {
            this.MainWindow?.OnAltLetter(vk);
        }

        void IAppHost.OnAlt()
        {
            this.MainWindow?.OnAlt();
        }

        void IAppHost.TabCycleStop()
        {
            this.ActiveTabWorkspace?.TabCycleStop();
        }

        void IAppHost.TabCycleNext()
        {
            this.ActiveTabWorkspace?.TabCycleNext();
        }

        void IAppHost.TabCyclePrev()
        {
            this.ActiveTabWorkspace?.TabCyclePrev();
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

        void Api.IApp.GrabProcess(int id)
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
