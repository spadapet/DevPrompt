using DevPrompt.UI.Settings;
using System.Diagnostics;
using System.Windows;

namespace DevPrompt.UI.ViewModels
{
    internal enum SettingsTabType
    {
        Consoles,
        Grab,
        Tools,
        Links,

        Default = Consoles,
    }

    /// <summary>
    /// View model for a settings tab
    /// </summary>
    internal class SettingsTabVM : Api.PropertyNotifier
    {
        public SettingsTabType TabType { get; }
        private SettingsDialogVM viewModel;
        private UIElement viewElement;

        public SettingsTabVM(SettingsDialogVM viewModel, SettingsTabType tabType)
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
                    case SettingsTabType.Consoles: return Resources.SettingsTabType_Consoles;
                    case SettingsTabType.Grab: return Resources.SettingsTabType_Grab;
                    case SettingsTabType.Links: return Resources.SettingsTabType_Links;
                    case SettingsTabType.Tools: return Resources.SettingsTabType_Tools;

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
                        case SettingsTabType.Consoles:
                            this.viewElement = new ConsolesSettingsControl(this.viewModel);
                            break;

                        case SettingsTabType.Grab:
                            this.viewElement = new GrabSettingsControl(this.viewModel);
                            break;

                        case SettingsTabType.Links:
                            this.viewElement = new LinksSettingsControl(this.viewModel);
                            break;

                        case SettingsTabType.Tools:
                            this.viewElement = new ToolsSettingsControl(this.viewModel);
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
