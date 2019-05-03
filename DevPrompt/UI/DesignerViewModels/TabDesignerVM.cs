using DevPrompt.UI.ViewModels;
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
    public class TabDesignerVM : ITabVM
    {
        public string TabName { get; set; } = "Tab Name";
        public string ExpandedTabName { get; set; } = "Tab Name";
        public string Title { get; set; } = "Title";
        public bool Active { get; set; }

        public UIElement ViewElement { get; set; } = new Border()
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

        public ICommand ActivateCommand => null;
        public ICommand CloneCommand => null;
        public ICommand CloseCommand => null;
        public ICommand DetachCommand => null;
        public ICommand DefaultsCommand => null;
        public ICommand PropertiesCommand => null;
        public ICommand SetTabNameCommand => null;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged { add { } remove { } }

        public void Focus()
        {
        }
    }
}
