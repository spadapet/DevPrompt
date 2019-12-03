using DevPrompt.Settings;
using System;
using System.Collections.Generic;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the settings import dialog
    /// </summary>
    internal sealed class SettingsImportDialogVM : Api.Utility.PropertyNotifier
    {
        private readonly AppSettings settings;
        private int consolesIndex;
        private int grabIndex;
        private int linksIndex;
        private int toolsIndex;
        private int tabThemesIndex;
        public IList<string> ImportChoices { get; }

        public SettingsImportDialogVM(AppSettings settings)
        {
            this.settings = settings;
            this.ImportChoices = new string[]
                {
                    Resources.ImportDialog_ChoiceReplace,
                    Resources.ImportDialog_ChoiceAdd,
                    Resources.ImportDialog_ChoiceNone,
                };
        }

        private int FilterIndex(int index) => Math.Min(this.ImportChoices.Count - 1, Math.Max(0, index));

        public int ConsolesIndex
        {
            get => this.consolesIndex;
            set => this.SetPropertyValue(ref this.consolesIndex, this.FilterIndex(value));
        }

        public int GrabIndex
        {
            get => this.grabIndex;
            set => this.SetPropertyValue(ref this.grabIndex, this.FilterIndex(value));
        }

        public int LinksIndex
        {
            get => this.linksIndex;
            set => this.SetPropertyValue(ref this.linksIndex, this.FilterIndex(value));
        }

        public int ToolsIndex
        {
            get => this.toolsIndex;
            set => this.SetPropertyValue(ref this.toolsIndex, this.FilterIndex(value));
        }

        public int TabThemesIndex
        {
            get => this.tabThemesIndex;
            set => this.SetPropertyValue(ref this.tabThemesIndex, this.FilterIndex(value));
        }

        public void Import(AppSettings targetSettings)
        {
            if (this.ConsolesIndex == 0)
            {
                targetSettings.Consoles.Clear();
            }

            if (this.ConsolesIndex != 2)
            {
                foreach (ConsoleSettings setting in this.settings.Consoles)
                {
                    targetSettings.Consoles.Add(setting.Clone());
                }
            }

            if (this.GrabIndex == 0)
            {
                targetSettings.GrabConsoles.Clear();
            }

            if (this.GrabIndex != 2)
            {
                foreach (GrabConsoleSettings setting in this.settings.GrabConsoles)
                {
                    targetSettings.GrabConsoles.Add(setting.Clone());
                }
            }

            if (this.LinksIndex == 0)
            {
                targetSettings.Links.Clear();
            }

            if (this.LinksIndex != 2)
            {
                foreach (LinkSettings setting in this.settings.Links)
                {
                    targetSettings.Links.Add(setting.Clone());
                }
            }

            if (this.ToolsIndex == 0)
            {
                targetSettings.Tools.Clear();
            }

            if (this.ToolsIndex != 2)
            {
                foreach (ToolSettings setting in this.settings.Tools)
                {
                    targetSettings.Tools.Add(setting.Clone());
                }
            }

            if (this.TabThemesIndex == 0)
            {
                targetSettings.TabThemes.Clear();
                targetSettings.HasDefaultThemeKeys = this.settings.HasDefaultThemeKeys;
            }

            if (this.TabThemesIndex == 1 && this.settings.TabThemes.Count > 0)
            {
                targetSettings.HasDefaultThemeKeys = targetSettings.HasDefaultThemeKeys && this.settings.HasDefaultThemeKeys;
            }

            if (this.TabThemesIndex != 2)
            {
                foreach (TabTheme tabTheme in this.settings.TabThemes)
                {
                    targetSettings.TabThemes.Add(tabTheme.Clone());
                }
            }

            targetSettings.EnsureValid();
        }
    }
}
