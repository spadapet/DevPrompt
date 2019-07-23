using DevPrompt.Api;
using JSONTools.UI;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace JSONTools
{
    [Guid("ab69a7a5-d5da-43a0-82b0-06132ba57453")]
    internal sealed class JSONToolsTab : PropertyNotifier, ITab, IDisposable
    {
        public IWindow Window { get; }
        public IWorkspace Workspace { get; }
        private UIElement viewElement;

        public JSONToolsTab(IWindow window, IWorkspace workspace)
        {
            this.Window = window;
            this.Workspace = workspace;
        }

        public void Dispose()
        {
            this.ViewElement = null;
        }

        public Guid Id => this.GetType().GUID;
        public string Name => Resources.JSONToolsTabName;
        public string Tooltip => string.Empty;
        public string Title => this.Name;

        public UIElement ViewElement
        {
            get
            {
                if (this.viewElement == null)
                {
                    this.viewElement = new Tools(this);
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
                Action action = () => this.viewElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
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

        // Doesn't save state (yet)
        ITabSnapshot ITab.Snapshot => null;

        // Hide some unrelated context menu items
        ICommand ITab.CloneCommand => null;
        ICommand ITab.DetachCommand => null;
        ICommand ITab.DefaultsCommand => null;
        ICommand ITab.PropertiesCommand => null;
        ICommand ITab.SetTabNameCommand => null;
    }
}
