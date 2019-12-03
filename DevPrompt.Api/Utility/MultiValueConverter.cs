using System;
using System.Globalization;
using System.Windows.Data;

namespace DevPrompt.Api.Utility
{
    public abstract class MultiValueConverter : IMultiValueConverter
    {
        public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}
