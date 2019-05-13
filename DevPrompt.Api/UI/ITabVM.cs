using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.Api
{
    /// <summary>
    /// View model to wrap ITab model
    /// </summary>
    public interface ITabVM : INotifyPropertyChanged, IDisposable
    {
        Guid Id { get; }
        string Name { get; }
        string Tooltip { get; }
        string Title { get; }
        UIElement ViewElement { get; }
        ITabSnapshot Snapshot { get; }
        ActiveState ActiveState { get; set; }
        bool CreatedTab { get; }
        ITab Tab { get; }

        ICommand ActivateCommand { get; }
        ICommand CloseCommand { get; }
        ICommand CloneCommand { get; }
        ICommand DetachCommand { get; }
        ICommand DefaultsCommand { get; }
        ICommand PropertiesCommand { get; }
        ICommand SetTabNameCommand { get; }

        void Focus();
        bool TakeRestoredTab(ITab tab);
    }
}
