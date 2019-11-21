using System;
using System.Windows;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class NullToCollapsedConverter : DelegateConverter
    {
        public NullToCollapsedConverter()
            : base(NullToCollapsedConverter.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            return (value != null) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
