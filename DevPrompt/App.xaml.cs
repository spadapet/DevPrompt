using DevPrompt.Interop;
using DevPrompt.Settings;
using DevPrompt.UI;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DevPrompt
{
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

        private async void OnStartup(object sender, StartupEventArgs args)
        {
            DevPrompt.Interop.App.CreateApp(this, out this.nativeApp);

            this.MainWindow = new MainWindow(this.Settings);

            this.Settings.CopyFrom(await AppSettings.Load(AppSettings.DefaultPath));
            this.MainWindow.Show();
        }

        private void OnExit(object sender, ExitEventArgs args)
        {
            this.nativeApp?.Dispose();
            Marshal.FinalReleaseComObject(this.nativeApp);
            this.nativeApp = null;
        }

        void IAppHost.OnProcessOpening(IProcess process, bool activate, string path)
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.OnProcessOpening(process, activate, path);
            }
        }

        void IAppHost.OnProcessClosing(IProcess process)
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.OnProcessClosing(process);
            }
        }

        void IAppHost.OnProcessEnvChanged(IProcess process, string env)
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.OnProcessEnvChanged(process, env);
            }
        }

        void IAppHost.OnProcessTitleChanged(IProcess process, string title)
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.OnProcessTitleChanged(process, title);
            }
        }

        void IAppHost.CloneActiveProcess()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.ActiveProcess?.CloneCommand.Execute(null);
            }
        }

        void IAppHost.SetTabName()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.ActiveProcess?.SetTabNameCommand.Execute(null);
            }
        }

        void IAppHost.CloseActiveProcess()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.ActiveProcess?.CloseCommand.Execute(null);
            }
        }

        void IAppHost.DetachActiveProcess()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.ActiveProcess?.DetachCommand.Execute(null);
            }
        }

        IntPtr IAppHost.GetMainWindow()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                WindowInteropHelper helper = new WindowInteropHelper(mainWindow);
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
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.OnAltLetter(vk);
            }
        }

        void IAppHost.OnAlt()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.OnAlt();
            }
        }

        void IAppHost.TabCycleStop()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.TabCycleStop();
            }
        }

        void IAppHost.TabCycleNext()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.TabCycleNext();
            }
        }

        void IAppHost.TabCyclePrev()
        {
            if (this.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ViewModel.TabCyclePrev();
            }
        }
    }
}
