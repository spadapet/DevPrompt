using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class KeyColorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }
        public DataTemplate EmptyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Api.ITabThemeKey themeKey)
            {
                return (themeKey.ThemeKeyColor == default) ? this.DefaultTemplate : this.ColorTemplate;
            }
            else if (item is Color color)
            {
                return (color == default) ? this.DefaultTemplate : this.ColorTemplate;
            }
            else if (item is string stringColor)
            {
                return (WpfUtility.ColorFromString(stringColor) == default) ? this.DefaultTemplate : this.ColorTemplate;
            }

            return this.EmptyTemplate;
        }
    }
}
