using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace DevPrompt.Api
{
    public interface IWorkspace : INotifyPropertyChanged
    {
        Guid Id { get; }
        string Name { get; }
        string Tooltip { get; }
        string Title { get; }
        UIElement ViewElement { get; }
        IEnumerable<MenuItem> MenuItems { get; }
        IWorkspaceSnapshot Snapshot { get; }

        void Focus();
        void OnShowing();
        void OnHiding();
        void OnWindowActivated();
        void OnWindowDeactivated();
    }
}
