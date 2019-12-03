using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class NullToCollapsedConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
