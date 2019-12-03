using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class BoolToCollapsedConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }

            throw new InvalidOperationException();
        }
    }
}
