using DevOps.UI.ViewModels;
using DevPrompt.Plugins;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Windows.Controls;

namespace DevOps
{
    [Export(typeof(IMenuItemProvider))]
    public class MenuItemProvider : IMenuItemProvider
    {
        public MenuItemProvider()
        {
        }

        IEnumerable<MenuItem> IMenuItemProvider.GetMenuItems(MenuType menu, IMainWindowVM window)
        {
            switch (menu)
            {
                case MenuType.Tools:
                    yield return new MenuItem()
                    {
                        Header = "Pull request dashboard",
                        Command = new DelegateCommand(obj => this.OnPullRequestDashboard((IMainWindowVM)obj)),
                        CommandParameter = window
                    };
                    break;
            }
        }

        private void OnPullRequestDashboard(IMainWindowVM window)
        {
            PullRequestTabVM tab = window.Tabs.OfType<PullRequestTabVM>().FirstOrDefault();
            if (tab != null)
            {
                window.ActiveTab = tab;
            }
            else
            {
                tab = new PullRequestTabVM(window);
                window.AddTab(tab, activate: true);
            }
        }
    }
}
