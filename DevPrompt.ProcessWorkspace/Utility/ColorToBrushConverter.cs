using System;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class ColorToBrushConverter : DelegateConverter
    {
        public ColorToBrushConverter()
            : base(ColorToBrushConverter.Convert)
        {
        }

        public static object Convert(object value, Type targetType, object parameter)
        {
            return (value is Color color && color != default) ? new SolidColorBrush(color) : null;
        }
    }
}
