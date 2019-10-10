using DevPrompt.ProcessWorkspace.UI.ViewModels;
using DevPrompt.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

namespace DevPrompt.ProcessWorkspace
{
    /// <summary>
    /// Manages the process tabs as processes are created and closed.
    /// </summary>
    [Export(typeof(Api.IProcessListener))]
    internal class ProcessListener : Api.IProcessListener
    {
        private Api.IApp app;

        [ImportingConstructor]
        public ProcessListener(Api.IApp app)
        {
            this.app = app;
        }

        public void OnProcessOpening(Api.IProcess process, bool activate, string path)
        {
            foreach (Tuple<Api.IWindow, Api.IProcessWorkspace> pair in this.ProcessWorkspaces)
            {
                Api.IWindow window = pair.Item1;
                Api.IProcessWorkspace workspace = pair.Item2;

                if (workspace.ProcessHost is Api.IProcessHost processHost && processHost.ContainsProcess(process))
                {
                    ProcessTab tab = new ProcessTab(window, workspace, process)
                    {
                        RawName = this.app.Settings.GetDefaultTabName(path)
                    };

                    TabVM tabVM = workspace.Tabs.OfType<TabVM>().FirstOrDefault(t => t.TakeRestoredTab(tab));
                    if (tabVM != null)
                    {
                        if (activate)
                        {
                            workspace.ActiveTab = tabVM;
                        }
                    }
                    else
                    {
                        // No takers, so make a new tab
                        workspace.AddTab(tab, activate || !workspace.Tabs.Any());
                    }

                    break;
                }
            }
        }

        public void OnProcessClosing(Api.IProcess process)
        {
            foreach (Tuple<Api.IWindow, Api.IProcessWorkspace> pair in this.ProcessWorkspaces)
            {
                Api.IProcessWorkspace workspace = pair.Item2;

                if (workspace.FindTab(process) is TabVM tab)
                {
                    workspace.RemoveTab(tab);
                    break;
                }
            }
        }

        private IEnumerable<Tuple<Api.IWindow, Api.IProcessWorkspace>> ProcessWorkspaces
        {
            get
            {
                foreach (Api.IWindow window in this.app.Windows)
                {
                    foreach (Api.IWorkspaceHolder workspaceHolder in window.Workspaces)
                    {
                        if (workspaceHolder.Id == Api.Constants.ProcessWorkspaceId && workspaceHolder.Workspace is Api.IProcessWorkspace workspace)
                        {
                            yield return Tuple.Create(window, workspace);
                        }
                    }
                }
            }
        }
    }
}
