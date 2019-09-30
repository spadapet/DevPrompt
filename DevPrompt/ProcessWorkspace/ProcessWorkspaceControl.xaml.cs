using DevPrompt.UI.Controls;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DevPrompt.ProcessWorkspace
{
    internal partial class ProcessWorkspaceControl : UserControl, DragItemsControl.IDragHost
    {
        public ProcessWorkspace ViewModel { get; }

        public ProcessWorkspaceControl(ProcessWorkspace workspace)
        {
            this.ViewModel = workspace;
            this.InitializeComponent();

            this.ProcessHostWindow = new ProcessHostWindow(workspace.Window.App);
        }

        public ProcessHostWindow ProcessHostWindow
        {
            get => this.processHostHolder?.Child as ProcessHostWindow;
            set => this.processHostHolder.Child = value;
        }

        public UIElement ViewElement
        {
            get
            {
                return this.viewElementHolder.Child;
            }

            set
            {
                if (this.viewElementHolder.Child != value)
                {
                    this.viewElementHolder.Child = value;

                    if (value == null)
                    {
                        this.processHostHolder.Visibility = Visibility.Visible;
                        this.viewElementHolder.Visibility = Visibility.Collapsed;

                        this.ProcessHostWindow?.Show();
                    }
                    else
                    {
                        this.ProcessHostWindow?.Hide();

                        this.processHostHolder.Visibility = Visibility.Collapsed;
                        this.viewElementHolder.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void OnTabButtonMouseDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Middle && sender is Button button && button.DataContext is ITabVM tab)
            {
                tab.CloseCommand?.SafeExecute();
            }
        }

        private void OnTabButtonMouseMoveEvent(object sender, MouseEventArgs args)
        {
            if (sender is Button button)
            {
                this.tabItemsControl.NotifyMouseMove(button, args);
            }
        }

        private void OnTabButtonMouseCaptureEvent(object sender, MouseEventArgs args)
        {
            if (sender is Button button)
            {
                this.tabItemsControl.NotifyMouseCapture(button, args);
            }
        }

        private void OnTabButtonLoaded(object sender, RoutedEventArgs args)
        {
            if (sender is Button button)
            {
                this.ViewModel.AddTabButton(button);
            }
        }

        private void OnTabButtonUnloaded(object sender, RoutedEventArgs args)
        {
            if (sender is Button button)
            {
                this.ViewModel.RemoveTabButton(button);
            }
        }

        private void OnTabContextMenuOpened(object sender, RoutedEventArgs args)
        {
            if (sender is ContextMenu menu)
            {
                this.ViewModel.UpdateContextMenu(menu);
            }
        }

        private void OnTabContextMenuClosed(object sender, RoutedEventArgs args)
        {
            if (sender is ContextMenu menu)
            {
                menu.Placement = PlacementMode.MousePoint;
                menu.PlacementTarget = null;
            }
        }

        private void OnTabButtonContextMenuOpening(object sender, ContextMenuEventArgs args)
        {
            if (sender is Button button)
            {
                this.ViewModel.EnsureTab(button);
            }
        }

        void DragItemsControl.IDragHost.OnDrop(ItemsControl source, object droppedModel, int droppedIndex, bool copy)
        {
            if (source == this.tabItemsControl && droppedModel is ITabVM tab)
            {
                this.ViewModel.OnDrop(tab, droppedIndex, copy);
            }
        }

        /// <summary>
        /// Allows cloning of a process
        /// </summary>
        bool DragItemsControl.IDragHost.CanDropCopy(object droppedModel)
        {
            return (droppedModel is ITabVM tab) && (tab.Tab is ProcessTab);
        }
    }
}
