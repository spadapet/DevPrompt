using DevPrompt.Settings;
using DevPrompt.UI.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the plugins dialog
    /// </summary>
    internal class PluginsDialogVM : Api.PropertyNotifier
    {
        public AppSettings Settings { get; }
        private MainWindow window;
        private PluginsDialog dialog;
        private PluginsTabVM[] tabs;
        private PluginsTabVM activeTab;

        public PluginsDialogVM()
            : this(null, null, null, PluginsTabType.Default)
        {
        }

        public PluginsDialogVM(MainWindow window, PluginsDialog dialog, AppSettings settings, PluginsTabType activeTabType)
        {
            this.window = window;
            this.dialog = dialog;
            this.Settings = settings?.Clone();

            this.tabs = new PluginsTabVM[]
                {
                    new PluginsTabVM(this, PluginsTabType.PluginInstalled),
                    new PluginsTabVM(this, PluginsTabType.PluginAvailable),
                    new PluginsTabVM(this, PluginsTabType.PluginUpdates),
                };

            this.ActiveTabType = activeTabType;
        }

        public IList<PluginsTabVM> Tabs => this.tabs;

        public PluginsTabType ActiveTabType
        {
            get => this.activeTab.TabType;

            set
            {
                foreach (PluginsTabVM tab in this.Tabs)
                {
                    if (tab.TabType == value)
                    {
                        this.ActiveTab = tab;
                        break;
                    }
                }
            }
        }

        public PluginsTabVM ActiveTab
        {
            get => this.activeTab;

            set
            {
                if (this.SetPropertyValue(ref this.activeTab, value ?? this.Tabs.First()))
                {
                    this.OnPropertyChanged(nameof(this.ActiveTabType));
                }
            }
        }
    }
}
