using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class BoolToCollapsedConverter : DelegateConverter
    {
        public BoolToCollapsedConverter()
            : base(BoolToCollapsedConverter.Convert)
        {
        }

        public static object Convert(object value, Type targetType, object parameter)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }

            throw new InvalidOperationException();
        }
    }
}
