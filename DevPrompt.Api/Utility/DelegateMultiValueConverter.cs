using System;
using System.Globalization;
using System.Windows.Data;

namespace DevPrompt.Api.Utility
{
    public sealed class DelegateMultiValueConverter : IMultiValueConverter
    {
        public delegate object ConvertFunc(object[] values, Type targetType, object parameter, CultureInfo culture);
        public delegate object[] ConvertBackFunc(object value, Type[] targetTypes, object parameter, CultureInfo culture);

        private readonly DelegateMultiValueConverter.ConvertFunc convert;
        private readonly DelegateMultiValueConverter.ConvertBackFunc convertBack;

        public DelegateMultiValueConverter(DelegateMultiValueConverter.ConvertFunc convert, DelegateMultiValueConverter.ConvertBackFunc convertBack = null)
        {
            this.convert = convert;
            this.convertBack = convertBack;
        }

        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (this.convert != null)
                ? this.convert(values, targetType, parameter, culture)
                : throw new InvalidOperationException();
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return (this.convertBack != null)
                ? this.convertBack(value, targetTypes, parameter, culture)
                : throw new InvalidOperationException();
        }
    }
}
