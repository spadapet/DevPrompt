using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DevPrompt.Utility
{
    internal static class WpfUtility
    {
        public static T FindVisualAncestor<T>(DependencyObject item, bool includeSelf = false) where T : class
        {
            if (item is Visual)
            {
                for (DependencyObject parent = item != null ? (includeSelf ? item : VisualTreeHelper.GetParent(item)) : null;
                    parent != null;
                    parent = VisualTreeHelper.GetParent(parent))
                {
                    if (parent is T typedParent)
                    {
                        return typedParent;
                    }
                }
            }

            return null;
        }

        public static T FindItemContainer<T>(ItemsControl control, DependencyObject child, bool includeSelf) where T : DependencyObject
        {
            T parent = null;

            if (control.IsAncestorOf(child))
            {
                parent = WpfUtility.FindVisualAncestor<T>(child, includeSelf: true);
                while (parent != null && control.ItemContainerGenerator.ItemFromContainer(parent) == DependencyProperty.UnsetValue)
                {
                    parent = WpfUtility.FindVisualAncestor<T>(parent, includeSelf: false);
                }
            }

            return parent;
        }

        public static void CheckAccess(this Dispatcher dispatcher)
        {
            if (dispatcher.Thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Debug.Fail("Using Dispatcher from wrong thread");
                throw new InvalidOperationException(Resources.Exception_WrongDispatcherThread);
            }
        }

        public static void BeginOrRun(this Dispatcher dispatcher, Action action)
        {
            if (action != null)
            {
                if (dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    action();
                }
                else
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
                }
            }
        }
    }
}
