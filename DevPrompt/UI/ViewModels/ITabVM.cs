using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    public interface ITabVM : Api.ITabHolder, INotifyPropertyChanged, IDisposable
    {
        string Name { get; }
        string Tooltip { get; }
        string Title { get; }
        UIElement ViewElement { get; }
        Api.ITabSnapshot Snapshot { get; }
        IEnumerable<FrameworkElement> ContextMenuItems { get; }

        ICommand ActivateCommand { get; }
        ICommand CloseCommand { get; }
        ICommand CloseAllButThisCommand { get; }

        void Focus();
        bool TakeRestoredTab(Api.ITab tab);
    }
}
