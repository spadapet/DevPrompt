using System;
using System.Globalization;

namespace DevPrompt.ProcessWorkspace.Utility
{
    internal sealed class VisibleToBoolConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Api.ActiveState state)
            {
                return state == Api.ActiveState.Visible;
            }

            throw new InvalidOperationException();
        }
    }
}
