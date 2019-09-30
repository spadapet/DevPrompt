using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
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
    internal class TabDesignerVM : ITabVM, Api.ITab
    {
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        public Guid Id => Guid.Empty;
        public string Name => "Tab Name";
        public string Tooltip => "Tooltip";
        public string Title => "Title";
        public bool CreatedTab => true;
        public Api.ITab Tab => this;
        public Api.ITabSnapshot Snapshot => null;
        public IEnumerable<FrameworkElement> ContextMenuItems => null;
        public Api.ActiveState ActiveState { get; set; }

        public ICommand ActivateCommand => new DelegateCommand();
        public ICommand CloseCommand => new DelegateCommand();
        public ICommand CloseAllButThisCommand => new DelegateCommand();

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
