using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.UI.Settings;
using System;
using System.Diagnostics;
using System.Windows;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for a settings tab
    /// </summary>
    internal class SettingsTabVM : PropertyNotifier, IDisposable
    {
        public Api.SettingsTabType TabType { get; }
        private readonly SettingsDialogVM viewModel;
        private UIElement viewElement;

        public SettingsTabVM(SettingsDialogVM viewModel, Api.SettingsTabType tabType)
        {
            this.TabType = tabType;
            this.viewModel = viewModel;
        }

        public void Dispose()
        {
            if (this.viewElement is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public string Name
        {
            get
            {
                switch (this.TabType)
                {
                    case Api.SettingsTabType.Consoles: return Resources.SettingsTabType_Consoles;
                    case Api.SettingsTabType.Grab: return Resources.SettingsTabType_Grab;
                    case Api.SettingsTabType.Links: return Resources.SettingsTabType_Links;
                    case Api.SettingsTabType.Tools: return Resources.SettingsTabType_Tools;
                    case Api.SettingsTabType.Colors: return Resources.SettingsTabType_Colors;
                    case Api.SettingsTabType.Plugins: return Resources.SettingsTabType_Plugins;
                    case Api.SettingsTabType.Telemetry: return Resources.SettingsTabType_Telemetry;

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
                        case Api.SettingsTabType.Consoles:
                            this.viewElement = new ConsolesSettingsControl(this.viewModel);
                            break;

                        case Api.SettingsTabType.Grab:
                            this.viewElement = new GrabSettingsControl(this.viewModel);
                            break;

                        case Api.SettingsTabType.Links:
                            this.viewElement = new LinksSettingsControl(this.viewModel);
                            break;

                        case Api.SettingsTabType.Tools:
                            this.viewElement = new ToolsSettingsControl(this.viewModel);
                            break;

                        case Api.SettingsTabType.Colors:
                            this.viewElement = new ColorsSettingsControl(this.viewModel);
                            break;

                        case Api.SettingsTabType.Plugins:
                            this.viewElement = new PluginSettingsControl(this.viewModel);
                            break;

                        case Api.SettingsTabType.Telemetry:
                            this.viewElement = new TelemetrySettingsControl(this.viewModel);
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
