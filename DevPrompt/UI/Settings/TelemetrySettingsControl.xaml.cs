using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI.Settings
{
    internal partial class TelemetrySettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }

        public TelemetrySettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }
    }
}
