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

        private static object Convert(object value, Type targetType, object parameter)
        {
            if (value is Api.ITabThemeKey themeKey && themeKey.ThemeKeyColor != default)
            {
                return new SolidColorBrush(themeKey.ThemeKeyColor);
            }
            else if (value is Color color && color != default)
            {
                return new SolidColorBrush(color);
            }
            else if (value is string stringColor)
            {
                return new SolidColorBrush(WpfUtility.ColorFromString(stringColor));
            }

            return null;
        }
    }
}
