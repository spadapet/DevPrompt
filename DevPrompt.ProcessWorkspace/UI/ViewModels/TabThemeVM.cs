using System.Windows;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.UI.ViewModels
{
    internal class TabThemeVM
    {
        public string Header { get; }
        public Color KeyColor { get; }
        public Api.ITabTheme Theme { get; }
        public Brush BackgroundBrush => this.Theme.BackgroundSelectedBrush;
        public Visibility DefaultVisibility => (this.KeyColor == default) ? Visibility.Visible : Visibility.Collapsed;

        public TabThemeVM(string header, Color keyColor, Api.ITabTheme theme)
        {
            this.Header = header;
            this.KeyColor = keyColor;
            this.Theme = theme;
        }
    }
}
