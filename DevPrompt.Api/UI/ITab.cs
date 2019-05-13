using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.Api
{
    /// <summary>
    /// Any tab in a tabbed workspace
    /// </summary>
    public interface ITab : INotifyPropertyChanged
    {
        Guid Id { get; }
        string Name { get; }
        string Tooltip { get; }
        string Title { get; }
        UIElement ViewElement { get; }
        ITabSnapshot Snapshot { get; }

        void Focus();
        void OnShowing();
        void OnHiding();

        /// <summary>
        /// Return false to prevent RemoveTab from being called
        /// </summary>
        bool OnClosing();

        // Tab context menu commands. Any of them can return null if the command doesn't make sense.
        ICommand CloneCommand { get; }
        ICommand DetachCommand { get; }
        ICommand DefaultsCommand { get; }
        ICommand PropertiesCommand { get; }
        ICommand SetTabNameCommand { get; }
    }
}
