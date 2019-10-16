using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    public interface IWorkspaceVM : Api.IWorkspaceHolder, INotifyPropertyChanged, IDisposable
    {
        string Name { get; }
        string Tooltip { get; }
        string Title { get; }
        UIElement ViewElement { get; }
        IEnumerable<FrameworkElement> MenuItems { get; }
        IEnumerable<KeyBinding> KeyBindings { get; }
        Api.IWorkspaceSnapshot Snapshot { get; }
        ICommand ActivateCommand { get; }

        void Focus();
    }
}
