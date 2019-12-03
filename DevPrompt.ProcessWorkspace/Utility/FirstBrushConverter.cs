using System;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.Utility
{
    internal sealed class FirstBrushConverter : Api.Utility.MultiValueConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.OfType<Brush>().FirstOrDefault();
        }
    }
}
