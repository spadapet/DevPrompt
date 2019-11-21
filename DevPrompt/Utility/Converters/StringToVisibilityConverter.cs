using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class StringToVisibilityConverter : DelegateConverter
    {
        public StringToVisibilityConverter()
            : base(StringToVisibilityConverter.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            return (value is string str && !string.IsNullOrEmpty(str)) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
