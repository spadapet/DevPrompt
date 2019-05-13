using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevPrompt.Api
{
    /// <summary>
    /// View model to wrap IWorkspace model
    /// </summary>
    public interface IWorkspaceVM : INotifyPropertyChanged, IDisposable
    {
        Guid Id { get; }
        string Name { get; }
        string Tooltip { get; }
        string Title { get; }
        bool CreatedWorkspace { get; }
        UIElement ViewElement { get; }
        IEnumerable<MenuItem> MenuItems { get; }
        IWorkspaceSnapshot Snapshot { get; }
        ActiveState ActiveState { get; set; }
        IWorkspace Workspace { get; }

        ICommand ActivateCommand { get; }
    }
}
