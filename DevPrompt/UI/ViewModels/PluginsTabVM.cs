using DevPrompt.UI.Plugins;
using System.Diagnostics;
using System.Windows;

namespace DevPrompt.UI.ViewModels
{
    internal enum PluginsTabType
    {
        PluginInstalled,
        PluginAvailable,
        PluginUpdates,

        Default = PluginInstalled,
    }

    /// <summary>
    /// View model for a plugins tab
    /// </summary>
    internal class PluginsTabVM : Api.PropertyNotifier
    {
        public PluginsTabType TabType { get; }
        private PluginsDialogVM viewModel;
        private UIElement viewElement;

        public PluginsTabVM(PluginsDialogVM viewModel, PluginsTabType tabType)
        {
            this.TabType = tabType;
            this.viewModel = viewModel;
        }

        public string Name
        {
            get
            {
                switch (this.TabType)
                {
                    case PluginsTabType.PluginInstalled: return Resources.PluginsTabType_PluginInstalled;
                    case PluginsTabType.PluginAvailable: return Resources.PluginsTabType_PluginAvailable;
                    case PluginsTabType.PluginUpdates: return Resources.PluginsTabType_PluginUpdates;

                    default:
                        Debug.Fail($"Missing name for tab: {this.TabType}");
                        return "<?>";
                }
            }
        }

        public UIElement ViewElement
        {
            get
            {
                if (this.viewElement == null)
                {
                    switch (this.TabType)
                    {
                        case PluginsTabType.PluginInstalled:
                            this.viewElement = new InstalledPluginsControl(this.viewModel);
                            break;

                        case PluginsTabType.PluginAvailable:
                            this.viewElement = new AvailablePluginsControl(this.viewModel);
                            break;

                        case PluginsTabType.PluginUpdates:
                            this.viewElement = new UpdatePluginsControl(this.viewModel);
                            break;

                        default:
                            Debug.Fail($"Missing UI for tab: {this.TabType}");
                            break;
                    }
                }

                return this.viewElement;
            }
        }
    }
}
