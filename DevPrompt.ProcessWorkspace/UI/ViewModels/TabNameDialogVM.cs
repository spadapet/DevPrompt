using DevPrompt.ProcessWorkspace.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.UI.ViewModels
{
    /// <summary>
    /// View model for the tab name dialog
    /// </summary>
    internal class TabNameDialogVM : PropertyNotifier
    {
        public IReadOnlyList<Color> ThemeKeys { get; }
        private string name;
        private Color themeKeyColor;

        public TabNameDialogVM(Api.IAppSettings settings, string name, Color themeKeyColor)
        {
            this.ThemeKeys = settings.TabThemeKeys.ToList();
            this.name = name;
            this.themeKeyColor = themeKeyColor;
        }

        public string Name
        {
            get => this.name;
            set => this.SetPropertyValue(ref this.name, value ?? string.Empty);
        }

        public Color ThemeKeyColor
        {
            get => this.themeKeyColor;
            set => this.SetPropertyValue(ref this.themeKeyColor, value);
        }
    }
}
