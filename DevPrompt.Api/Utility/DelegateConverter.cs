using System;
using System.Globalization;
using System.Windows.Data;

namespace DevPrompt.Api.Utility
{
    public sealed class DelegateConverter : IValueConverter
    {
        public delegate object ConvertFunc(object value, Type targetType, object parameter, CultureInfo culture);

        private readonly DelegateConverter.ConvertFunc convert;
        private readonly DelegateConverter.ConvertFunc convertBack;

        public DelegateConverter(DelegateConverter.ConvertFunc convert, DelegateConverter.ConvertFunc convertBack = null)
        {
            this.convert = convert;
            this.convertBack = convertBack;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (this.convert != null)
                ? this.convert(value, targetType, parameter, culture)
                : throw new InvalidOperationException();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (this.convertBack != null)
                ? this.convertBack(value, targetType, parameter, culture)
                : throw new InvalidOperationException();
        }
    }
}
