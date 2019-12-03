using System;
using System.Globalization;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class DateToVisibilityConverter : Api.Utility.ValueConverter
    {
        private static readonly DateTime OldestDate = new DateTime(2019, 1, 1);

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is DateTime date && date >= DateToVisibilityConverter.OldestDate && date <= DateTime.Now) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
