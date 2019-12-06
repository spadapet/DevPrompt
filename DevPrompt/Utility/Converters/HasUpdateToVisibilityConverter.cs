using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class HasUpdateToVisibilityConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter is bool boolParameter && boolParameter;

            return (value is Api.AppUpdateState state && state == Api.AppUpdateState.HasUpdate)
                ? (inverse ? Visibility.Collapsed : Visibility.Visible)
                : (inverse ? Visibility.Visible : Visibility.Collapsed);
        }
    }
}
