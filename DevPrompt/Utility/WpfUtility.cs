using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
    }
}
