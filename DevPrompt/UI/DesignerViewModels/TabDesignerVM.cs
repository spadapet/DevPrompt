using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class TabDesignerVM : Api.ITabVM, Api.ITab
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        public Guid Id => Guid.Empty;
        public string Name => "Tab Name";
        public string Tooltip => "Tooltip";
        public string Title => "Title";
        public bool CreatedTab => true;
        public Api.ITab Tab => this;
        public Api.ITabSnapshot Snapshot => null;
        public Api.ActiveState ActiveState { get; set; }

        public ICommand ActivateCommand => new Api.DelegateCommand();
        public ICommand CloseCommand => new Api.DelegateCommand();
        public ICommand CloneCommand => new Api.DelegateCommand();
        public ICommand DetachCommand => new Api.DelegateCommand();
        public ICommand DefaultsCommand => new Api.DelegateCommand();
        public ICommand PropertiesCommand => new Api.DelegateCommand();
        public ICommand SetTabNameCommand => new Api.DelegateCommand();

        public TabDesignerVM(Api.ActiveState activeState = Api.ActiveState.Hidden)
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
                Text = "Tab Content",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            }
        };

        public void Focus()
        {
        }

        public bool TakeRestoredTab(Api.ITab tab)
        {
            return false;
        }

        public void OnShowing()
        {
        }

        public void OnHiding()
        {
        }

        public bool OnClosing()
        {
            return true;
        }
    }
}
