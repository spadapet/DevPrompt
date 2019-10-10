using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

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
        IEnumerable<FrameworkElement> ContextMenuItems { get; }

        void Focus();
        void OnShowing();
        void OnHiding();
        bool OnClosing(); // Global Ctrl+F4. Return false to prevent RemoveTab from being called
        void OnSetTabName(); // Global Ctrl+Shift+T
        void OnClone(); // Global Ctrl+T
        void OnDetach(); // Global Ctrl+Shift+F4
    }
}
