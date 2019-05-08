using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System.IO;

namespace DevPrompt.Interop
{
    internal class NativeProcessListener
    {
        private MainWindowVM window;

        public NativeProcessListener(MainWindowVM window)
        {
            this.window = window;
        }

        public void OnProcessOpening(IProcess process, bool activate, string path)
        {
            ProcessVM tab = new ProcessVM(this.window, process)
            {
                TabName = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "Tab"
            };

            foreach (GrabConsoleSettings grab in this.window.AppSettings.GrabConsoles)
            {
                if (grab.CanGrab(path))
                {
                    tab.TabName = grab.TabName;
                    break;
                }
            }

            this.window.AddTab(tab, activate);
        }

        public void OnProcessClosing(IProcess process)
        {
            if (this.window.FindProcess(process) is ProcessVM tab)
            {
                this.window.RemoveTab(tab);
            }
        }

        public void OnProcessEnvChanged(IProcess process, string env)
        {
            if (this.window.FindProcess(process) is ProcessVM tab)
            {
                tab.Env = env;
            }
        }

        public void OnProcessTitleChanged(IProcess process, string title)
        {
            if (this.window.FindProcess(process) is ProcessVM tab)
            {
                tab.Title = title;
            }
        }
    }
}
