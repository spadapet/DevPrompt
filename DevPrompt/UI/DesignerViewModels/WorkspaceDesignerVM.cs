using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class WorkspaceDesignerVM : Api.IWorkspaceVM, Api.IWorkspace
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        public Guid Id => Guid.Empty;
        public string Name => "Tab Name";
        public string Tooltip => "Tooltip";
        public string Title => "Title";
        public bool CreatedWorkspace => true;
        public Api.IWorkspace Workspace => this;
        public Api.IWorkspaceSnapshot Snapshot => null;
        public IEnumerable<MenuItem> MenuItems => Enumerable.Empty<MenuItem>();
        public Api.ActiveState ActiveState { get; set; }


        public ICommand ActivateCommand => new Api.DelegateCommand();

        public WorkspaceDesignerVM(Api.ActiveState activeState = Api.ActiveState.Hidden)
        {
            this.ActiveState = activeState;
        }

        public void Dispose()
        {
        }

        public UIElement ViewElement => new Border()
        {
            Background = new SolidColorBrush(Colors.SlateGray),
            Child = new TextBlock()
            {
                Padding = new Thickness(10),
                Text = "Workspace Content",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            }
        };

        void Api.IWorkspace.Focus()
        {
        }

        void Api.IWorkspace.OnShowing()
        {
        }

        void Api.IWorkspace.OnHiding()
        {
        }

        void Api.IWorkspace.OnWindowActivated()
        {
        }

        void Api.IWorkspace.OnWindowDeactivated()
        {
        }
    }
}
