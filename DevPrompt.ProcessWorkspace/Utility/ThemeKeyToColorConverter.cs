using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class ThemeKeyToColorConverter : Api.Utility.ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Api.ITabThemeKey themeKey && themeKey.ThemeKeyColor != default)
            {
                return themeKey.ThemeKeyColor;
            }
            else if (value is string stringColor)
            {
                return WpfUtility.ColorFromString(stringColor);
            }
            else if (value is IEnumerable<Api.ITabThemeKey> enumThemeKey)
            {
                return enumThemeKey.Select(t => t.ThemeKeyColor);
            }
            else if (value is IEnumerable<string> enumStringColor)
            {
                return enumStringColor.Select(s => WpfUtility.ColorFromString(s));
            }

            return value;
        }
    }
}
