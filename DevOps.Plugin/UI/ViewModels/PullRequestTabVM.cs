using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevOps.UI.ViewModels
{
    internal sealed class PullRequestTabVM : PropertyNotifier, ITabVM, IDisposable
    {
        public IMainWindowVM Window { get; }
        private bool active;
        private UIElement viewElement;

        public PullRequestTabVM(IMainWindowVM window)
        {
            this.Window = window;
        }

        public void Dispose()
        {
            this.ViewElement = null;
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
                    this.viewElement = new LoginPage(this);
                }

                return this.viewElement;
            }

            set
            {
                if (this.viewElement != value)
                {
                    if (this.viewElement is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    this.viewElement = value;
                    this.OnPropertyChanged(nameof(this.ViewElement));
                }
            }
        }

        public void Focus()
        {
            if (this.viewElement != null)
            {
                Action action = new Action(() => this.viewElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
                this.viewElement.Dispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle);
            }
        }

        public ICommand ActivateCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.Window.ActiveTab = this;
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.Window.RemoveTab(this);
                });
            }
        }

        public ICommand CloneCommand => null;
        public ICommand DetachCommand => null;
        public ICommand DefaultsCommand => null;
        public ICommand PropertiesCommand => null;
        public ICommand SetTabNameCommand => null;

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
        }
    }
}
