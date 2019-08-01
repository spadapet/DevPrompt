using System;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class ObjectToVisibilityConverter : DelegateConverter
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
