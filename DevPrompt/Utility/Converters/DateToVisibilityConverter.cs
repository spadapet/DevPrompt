using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class DateToVisibilityConverter : DelegateConverter
    {
        private static readonly DateTime OldestDate = new DateTime(2019, 1, 1);

        public DateToVisibilityConverter()
            : base(DateToVisibilityConverter.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            return (value is DateTime date && date >= DateToVisibilityConverter.OldestDate && date <= DateTime.Now) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
