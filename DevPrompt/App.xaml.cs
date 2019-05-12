using DevPrompt.Interop;
using DevPrompt.Plugins;
using DevPrompt.Settings;
using DevPrompt.UI;
using DevPrompt.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DevPrompt
{
    /// <summary>
    /// Singleton WPF app object (get using App.Current).
    /// The native code will call into here using the IAppHost interface. We call into
    /// native code using the IApp interface.
    /// </summary>
    internal partial class App : Application, IAppHost, Plugins.IApp
    {
        public AppSettings Settings { get; }
        public Interop.IApp NativeApp { get; private set; }

        private CompositionHost compositionHost;

        public App()
        {
            this.Settings = new AppSettings();

            this.Startup += this.OnStartup;
            this.Exit += this.OnExit;
        }

        public static new App Current
        {
            get
            {
                throw new NotImplementedException("Try not to use App.Current if possible");
                // return (App)Application.Current;
            }
        }

        public new MainWindow MainWindow
        {
            get
            {
                return base.MainWindow as MainWindow;
            }

            set
            {
                base.MainWindow = value;
            }
        }

        public ITabVM ActiveTab
        {
            get
            {
                return this.MainWindow?.ViewModel.ActiveTab;
            }
        }

        public T GetExport<T>()
        {
            if (this.compositionHost != null && this.compositionHost.TryGetExport<T>(out T value))
            {
                return value;
            }

            return default(T);
        }

        public IEnumerable<T> GetExports<T>()
        {
            List<T> exports = (this.compositionHost?.GetExports<T>() ?? Enumerable.Empty<T>()).ToList();
            exports.Sort((T x, T y) => string.Compare(x.GetType().Name, y.GetType().Name));

            return exports;
        }

        private async void OnStartup(object sender, StartupEventArgs args)
        {
            this.NativeApp = Interop.App.CreateApp(this, out string errorMessage);
            this.MainWindow = new MainWindow(this, this.Settings, errorMessage);
            this.MainWindow.Show();

            this.Settings.CopyFrom(await AppSettings.Load(AppSettings.DefaultPath));
            this.InitPlugins();
            this.MainWindow.OnAppInitComplete();

            foreach (IAppListener listener in this.GetExports<IAppListener>())
            {
                listener.OnStartup();
            }
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            foreach (IAppListener listener in this.GetExports<IAppListener>())
            {
                listener.OnExit();
            }

            if (this.compositionHost != null)
            {
                this.compositionHost.Dispose();
                this.compositionHost = null;
            }

            if (this.NativeApp != null)
            {
                this.NativeApp.Dispose();
                Marshal.FinalReleaseComObject(this.NativeApp);
                this.NativeApp = null;
            }
        }

        private void InitPlugins()
        {
            try
            {
                ConventionBuilder conventions = new ConventionBuilder();
                conventions.ForTypesDerivedFrom<Plugins.IApp>().Shared();
                conventions.ForTypesDerivedFrom<IAppListener>().Shared();

                this.compositionHost = new ContainerConfiguration()
                    .WithAssemblies(PluginUtility.GetPluginAssemblies(), conventions)
                    .WithProvider(new ExportProvider(this))
                    .CreateContainer();
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message, ex.StackTrace);
            }
        }

        void IAppHost.OnProcessOpening(IProcess process, bool activate, string path)
        {
            this.MainWindow?.NativeProcessListener.OnProcessOpening(process, activate, path);
        }

        void IAppHost.OnProcessClosing(IProcess process)
        {
            this.MainWindow?.NativeProcessListener.OnProcessClosing(process);
        }

        void IAppHost.OnProcessEnvChanged(IProcess process, string env)
        {
            this.MainWindow?.NativeProcessListener.OnProcessEnvChanged(process, env);
        }

        void IAppHost.OnProcessTitleChanged(IProcess process, string title)
        {
            this.MainWindow?.NativeProcessListener.OnProcessTitleChanged(process, title);
        }

        void IAppHost.CloneActiveProcess()
        {
            this.ActiveTab?.CloneCommand.Execute(null);
        }

        void IAppHost.SetTabName()
        {
            this.ActiveTab?.SetTabNameCommand.Execute(null);
        }

        void IAppHost.CloseActiveProcess()
        {
            this.ActiveTab?.CloseCommand.Execute(null);
        }

        void IAppHost.DetachActiveProcess()
        {
            this.ActiveTab?.DetachCommand.Execute(null);
        }

        IntPtr IAppHost.GetMainWindow()
        {
            if (this.MainWindow != null)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this.MainWindow);
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
            this.MainWindow?.ViewModel.TabCycleStop();
        }

        void IAppHost.TabCycleNext()
        {
            this.MainWindow?.ViewModel.TabCycleNext();
        }

        void IAppHost.TabCyclePrev()
        {
            this.MainWindow?.ViewModel.TabCyclePrev();
        }
    }
}
