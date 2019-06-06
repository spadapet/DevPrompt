using DevPrompt.UI.Controls;
using DevPrompt.Utility;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

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
            if (args.ChangedButton == MouseButton.Middle && sender is Button button && button.DataContext is Api.ITabVM tab)
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

        private void OnTabContextMenuOpened(object sender, RoutedEventArgs args)
        {
#if false
            if (sender is ContextMenu menu)
            {
                Action action = () =>
                {
                    foreach (HwndSource source in PresentationSource.CurrentSources.OfType<HwndSource>())
                    {
                        if (source.RootVisual is FrameworkElement rootElement && rootElement.Parent is Popup popup && popup.IsOpen && popup.Child is ContextMenu childMenu)
                        {
                            //SetFocus(source.Handle);
                        }
                    }
                };

                this.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
#endif
        }

        void DragItemsControl.IDragHost.OnDrop(ItemsControl source, object droppedModel, int droppedIndex, bool copy)
        {
            if (source == this.tabItemsControl && droppedModel is Api.ITabVM tab)
            {
                this.ViewModel.OnDrop(tab, droppedIndex, copy);
            }
        }

        /// <summary>
        /// Allows cloning of a process
        /// </summary>
        bool DragItemsControl.IDragHost.CanDropCopy(object droppedModel)
        {
            return droppedModel is Api.ITabVM tab && tab.CloneCommand?.CanExecute(null) == true;
        }
    }
}
