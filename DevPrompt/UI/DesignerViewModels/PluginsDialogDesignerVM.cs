using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System.Collections.Generic;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// View model for the plugins dialog
    /// </summary>
    internal class PluginsDialogDesignerVM
    {
        public AppSettings Settings { get; } = new AppSettings();
        public IList<IPluginVM> AvailablePlugins { get; } = new List<IPluginVM>()
        {
            new NuGetPluginDesignerVM(),
            new NuGetPluginDesignerVM(),
            new NuGetPluginDesignerVM(),
            new NuGetPluginDesignerVM(),
        };

        public PluginsDialogDesignerVM()
        {
        }
    }
}
