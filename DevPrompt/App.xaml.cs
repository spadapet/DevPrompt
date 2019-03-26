using DevPrompt.Interop;
using DevPrompt.Settings;
using DevPrompt.UI;
using System;
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
    internal partial class App : Application, IAppHost
    {
        public AppSettings Settings { get; }
        public IApp NativeApp => this.nativeApp;

        private IApp nativeApp;
        private static App designerApp;

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
                App app = (App)Application.Current;
                if (app == null)
                {
                    if (App.designerApp == null)
                    {
                        App.designerApp = new App();
                    }

                    app = App.designerApp;
                }

                return app;
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

        private async void OnStartup(object sender, StartupEventArgs args)
        {
            TypeLoadException nativeAppException = null;
            try
            {
                DevPrompt.Interop.App.CreateApp(this, out this.nativeApp);
            }
            catch (TypeLoadException ex)
            {
                nativeAppException = ex;
            }

            this.MainWindow = new MainWindow(this.Settings, nativeAppException?.Message);
            this.MainWindow.Show();

            this.Settings.CopyFrom(await AppSettings.Load(AppSettings.DefaultPath));
            this.MainWindow.OnAppInitComplete();
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            if (this.nativeApp != null)
            {
                this.nativeApp.Dispose();
                Marshal.FinalReleaseComObject(this.nativeApp);
                this.nativeApp = null;
            }
        }

        void IAppHost.OnProcessOpening(IProcess process, bool activate, string path)
        {
            this.MainWindow?.ViewModel.OnProcessOpening(process, activate, path);
        }

        void IAppHost.OnProcessClosing(IProcess process)
        {
            this.MainWindow?.ViewModel.OnProcessClosing(process);
        }

        void IAppHost.OnProcessEnvChanged(IProcess process, string env)
        {
            this.MainWindow?.ViewModel.OnProcessEnvChanged(process, env);
        }

        void IAppHost.OnProcessTitleChanged(IProcess process, string title)
        {
            this.MainWindow?.ViewModel.OnProcessTitleChanged(process, title);
        }

        void IAppHost.CloneActiveProcess()
        {
            this.MainWindow?.ViewModel.ActiveProcess?.CloneCommand.Execute(null);
        }

        void IAppHost.SetTabName()
        {
            this.MainWindow?.ViewModel.ActiveProcess?.SetTabNameCommand.Execute(null);
        }

        void IAppHost.CloseActiveProcess()
        {
            this.MainWindow?.ViewModel.ActiveProcess?.CloseCommand.Execute(null);
        }

        void IAppHost.DetachActiveProcess()
        {
            this.MainWindow?.ViewModel.ActiveProcess?.DetachCommand.Execute(null);
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
