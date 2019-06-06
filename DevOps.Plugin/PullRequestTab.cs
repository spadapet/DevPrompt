using DevOps.UI;
using DevPrompt.Api;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevOps
{
    [Guid("527dd4ee-5150-4b3b-84d0-c81e548b7830")]
    internal sealed class PullRequestTab : PropertyNotifier, ITab, IDisposable
    {
        public IWindow Window { get; }
        public IWorkspace Workspace { get; }

        private UIElement viewElement;

        public PullRequestTab(IWindow window, IWorkspace workspace)
        {
            this.Window = window;
            this.Workspace = workspace;
        }

        public void Dispose()
        {
            this.ViewElement = null;
        }

        public Guid Id
        {
            get
            {
                return this.GetType().GUID;
            }
        }

        public string Name
        {
            get
            {
                return "Pull Requests";
            }
        }

        public string Tooltip
        {
            get
            {
                return this.Title;
            }
        }

        public string Title
        {
            get
            {
                return this.Name;
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

        void ITab.Focus()
        {
            if (this.viewElement != null)
            {
                Action action = new Action(() => this.viewElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
                this.viewElement.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
        }

        void ITab.OnShowing()
        {
        }

        void ITab.OnHiding()
        {
        }

        bool ITab.OnClosing()
        {
            return true;
        }

        ICommand ITab.CloneCommand => null;
        ICommand ITab.DetachCommand => null;
        ICommand ITab.DefaultsCommand => null;
        ICommand ITab.PropertiesCommand => null;
        ICommand ITab.SetTabNameCommand => null;
        ITabSnapshot ITab.Snapshot => null; // doesn't save state (yet)
    }
}
