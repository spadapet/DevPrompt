using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class StringToVisibilityConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is string str && !string.IsNullOrEmpty(str)) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
