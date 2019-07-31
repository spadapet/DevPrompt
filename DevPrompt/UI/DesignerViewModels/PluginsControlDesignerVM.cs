using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System.Collections.Generic;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// View model for the plugins dialog
    /// </summary>
    internal class PluginsControlDesignerVM
    {
        public AppSettings Settings { get; } = new AppSettings();
        public IPluginVM CurrentPlugin
        {
            get => this.Plugins[0];
            set { }
        }

        public IList<IPluginVM> Plugins { get; } = new List<IPluginVM>()
        {
            new NuGetPluginDesignerVM(installed: true),
            new NuGetPluginDesignerVM(installed: true, updateAvailable: true),
            new NuGetPluginDesignerVM(),
            new NuGetPluginDesignerVM(busy: true),
        };

        public IList<PluginSortVM> Sorts { get; } = new List<PluginSortVM>()
        {
            new PluginSortVM("Installed", null),
            new PluginSortVM("Most Recent", null),
        };

        public PluginSortVM Sort => this.Sorts[0];
    }
}
