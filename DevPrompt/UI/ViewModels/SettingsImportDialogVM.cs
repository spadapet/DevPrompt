using DevPrompt.Settings;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the settings import dialog
    /// </summary>
    internal class SettingsImportDialogVM : PropertyNotifier
    {
        private AppSettings settings;
        private int consolesIndex;
        private int grabIndex;
        private int linksIndex;
        private int toolsIndex;
        private int pluginsIndex;
        public IList<string> ImportChoices { get; }

        public SettingsImportDialogVM(AppSettings settings)
        {
            this.settings = settings;
            this.ImportChoices = new string[]
                {
                    "Replace existing",
                    "Add to existing",
                    "Do not import",
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

        public int PluginsIndex
        {
            get => this.pluginsIndex;
            set => this.SetPropertyValue(ref this.pluginsIndex, this.FilterIndex(value));
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

            if (this.PluginsIndex == 0)
            {
                targetSettings.UserPluginDirectories.Clear();
            }

            if (this.PluginsIndex != 2)
            {
                foreach (PluginDirectorySettings setting in this.settings.UserPluginDirectories)
                {
                    targetSettings.UserPluginDirectories.Add(setting.Clone());
                }
            }

            targetSettings.EnsureValid();
        }
    }
}
