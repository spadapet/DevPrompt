using System;
using System.Windows;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class ObjectToVisibilityConverter : DelegateConverter
    {
        public ObjectToVisibilityConverter()
            : base(ObjectToVisibilityConverter.Convert)
        {
        }

        public static object Convert(object value, Type targetType, object parameter)
        {
            return (value != null) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
