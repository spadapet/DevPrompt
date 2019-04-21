using DevPrompt.Settings;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;

namespace DevPrompt.UI
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

        public int ConsolesIndex
        {
            get
            {
                return this.consolesIndex;
            }

            set
            {
                int newValue = Math.Min(this.ImportChoices.Count - 1, Math.Max(0, value));
                this.SetPropertyValue(ref this.consolesIndex, newValue);
            }
        }

        public int GrabIndex
        {
            get
            {
                return this.grabIndex;
            }

            set
            {
                int newValue = Math.Min(this.ImportChoices.Count - 1, Math.Max(0, value));
                this.SetPropertyValue(ref this.grabIndex, newValue);
            }
        }

        public int LinksIndex
        {
            get
            {
                return this.linksIndex;
            }

            set
            {
                int newValue = Math.Min(this.ImportChoices.Count - 1, Math.Max(0, value));
                this.SetPropertyValue(ref this.linksIndex, newValue);
            }
        }

        public int ToolsIndex
        {
            get
            {
                return this.toolsIndex;
            }

            set
            {
                int newValue = Math.Min(this.ImportChoices.Count - 1, Math.Max(0, value));
                this.SetPropertyValue(ref this.toolsIndex, newValue);
            }
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
                    targetSettings.Consoles.Add(setting);
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
                    targetSettings.GrabConsoles.Add(setting);
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
                    targetSettings.Links.Add(setting);
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
                    targetSettings.Tools.Add(setting);
                }
            }

            targetSettings.EnsureValid();
        }
    }
}
