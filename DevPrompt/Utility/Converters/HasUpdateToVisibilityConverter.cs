using DevPrompt.Api;
using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Windows;

namespace DevPrompt.Utility.Converters
{
    internal sealed class HasUpdateToVisibilityConverter : DelegateConverter
    {
        public HasUpdateToVisibilityConverter()
            : base(HasUpdateToVisibilityConverter.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            return (value is AppUpdateState state && state == AppUpdateState.HasUpdate) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
