using System;
using System.Globalization;
using System.Windows.Data;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public class DelegateMultiConverter : IMultiValueConverter
    {
        public delegate object ConvertFunc(object[] values, Type targetType, object parameter);
        public delegate object[] ConvertBackFunc(object value, Type[] targetTypes, object parameter);

        private readonly DelegateMultiConverter.ConvertFunc convert;
        private readonly DelegateMultiConverter.ConvertBackFunc convertBack;

        public DelegateMultiConverter(DelegateMultiConverter.ConvertFunc convert, DelegateMultiConverter.ConvertBackFunc convertBack = null)
        {
            this.convert = convert;
            this.convertBack = convertBack;
        }

        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (this.convert != null)
                ? this.convert(values, targetType, parameter)
                : throw new InvalidOperationException();
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return (this.convertBack != null)
                ? this.convertBack(value, targetTypes, parameter)
                : throw new InvalidOperationException();
        }
    }
}
