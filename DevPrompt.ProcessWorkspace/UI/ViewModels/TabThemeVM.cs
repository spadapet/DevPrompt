using System.Windows;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.UI.ViewModels
{
    internal class TabThemeVM
    {
        public string Header { get; }
        public Color ThemeKeyColor { get; }
        public Api.ITabTheme Theme { get; }
        public Brush BackgroundBrush => this.Theme.BackgroundSelectedBrush;
        public Visibility DefaultVisibility => (this.ThemeKeyColor == default) ? Visibility.Visible : Visibility.Collapsed;

        public TabThemeVM(string header, Color keyColor, Api.ITabTheme theme)
        {
            this.Header = header;
            this.ThemeKeyColor = keyColor;
            this.Theme = theme;
        }
    }
}
