using DevPrompt.UI.ViewModels;
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
