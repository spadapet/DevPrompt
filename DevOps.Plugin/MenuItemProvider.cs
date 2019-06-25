using DevPrompt.Api;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Windows.Controls;

namespace DevOps
{
    /// <summary>
    /// Adds custom menu items to the main window
    /// </summary>
    [Export(typeof(IMenuItemProvider))]
    public class MenuItemProvider : IMenuItemProvider
    {
        IEnumerable<MenuItem> IMenuItemProvider.GetMenuItems(MenuType menu, IWindow window)
        {
            switch (menu)
            {
                case MenuType.Tools:
                    yield return new MenuItem()
                    {
                        Header = "Pull request dashboard",
                        Command = new DelegateCommand(obj => this.OnPullRequestDashboard((IWindow)obj)),
                        CommandParameter = window
                    };
                    break;
            }
        }

        private void OnPullRequestDashboard(IWindow window)
        {
            // Adds a new tab or activates and existing tab in the default process workspace
            if (window.FindWorkspace(Constants.ProcessWorkspaceId) is IWorkspaceVM workspaceVM && workspaceVM.Workspace is ITabWorkspace workspace)
            {
                window.ActiveWorkspace = workspaceVM;

                if (workspace.Tabs.FirstOrDefault(t => t.Id == typeof(PullRequestTab).GUID) is ITabVM tabVM)
                {
                    // The tab was already open, make sure it's shown
                    workspace.ActiveTab = tabVM;
                }
                else
                {
                    ITab tab = new PullRequestTab(window, workspace);
                    workspace.AddTab(new TabVM(window, workspace, tab), activate: true);
                }
            }
        }
    }
}
