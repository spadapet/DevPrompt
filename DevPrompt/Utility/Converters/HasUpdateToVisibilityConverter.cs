using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class HasUpdateToVisibilityConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Api.AppUpdateState state && state == Api.AppUpdateState.HasUpdate) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
