using DevPrompt.Settings;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// Any tab's view model must implement this interface
    /// </summary>
    public interface ITabVM : INotifyPropertyChanged
    {
        void Focus();

        string TabName { get; }
        string ExpandedTabName { get; } // expands environment variables from TabName
        string Title { get; }
        bool Active { get; set; }
        UIElement ViewElement { get; }
        ITabSnapshot Snapshot { get; }

        // Tab context menu commands. Any of them can return null if the command doesn't make sense.

        ICommand ActivateCommand { get; }
        ICommand CloneCommand { get; }
        ICommand CloseCommand { get; }
        ICommand DetachCommand { get; }
        ICommand DefaultsCommand { get; }
        ICommand PropertiesCommand { get; }
        ICommand SetTabNameCommand { get; }
    }
}
