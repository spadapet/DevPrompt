using System.Windows;
using System.Windows.Input;
using DevOps.UI;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;

namespace DevOps.ViewModels
{
    internal class PullRequestDashboardVM : PropertyNotifier, ITabVM
    {
        private bool active;
        private PullRequestDashboard viewElement;
        private IMainWindowVM window;

        public PullRequestDashboardVM()
        {
        }

        public PullRequestDashboardVM(IMainWindowVM window)
        {
            this.window = window;
        }

        public string TabName
        {
            get
            {
                return "Pull Requests";
            }
        }


        public string ExpandedTabName
        {
            get
            {
                return this.TabName;
            }
        }

        public string Title
        {
            get
            {
                return this.TabName;
            }
        }

        public bool Active
        {
            get
            {
                return this.active;
            }

            set
            {
                this.SetPropertyValue(ref this.active, value);
            }
        }

        public UIElement ViewElement
        {
            get
            {
                if (this.viewElement == null)
                {
                    this.viewElement = new PullRequestDashboard(this);
                }

                return this.viewElement;
            }
        }

        public void Focus()
        {
        }

        public ICommand ActivateCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.window.ActiveTab = this;
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.window.RemoveTab(this);
                });
            }
        }

        public ICommand CloneCommand => null;
        public ICommand DetachCommand => null;
        public ICommand DefaultsCommand => null;
        public ICommand PropertiesCommand => null;
        public ICommand SetTabNameCommand => null;
    }
}
