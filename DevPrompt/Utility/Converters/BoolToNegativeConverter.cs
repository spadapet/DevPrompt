using System;
using System.Globalization;

namespace DevPrompt.Utility.Converters
{
    internal sealed class BoolToNegativeConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            throw new InvalidOperationException();
        }
    }
}
