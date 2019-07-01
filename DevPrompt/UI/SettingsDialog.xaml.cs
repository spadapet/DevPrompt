using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System.Windows;

namespace DevPrompt.UI
{
    internal partial class SettingsDialog : Window
    {
        public SettingsDialogVM ViewModel { get; }

        public SettingsDialog(MainWindow window, AppSettings settings, SettingsTabType activeTabType = SettingsTabType.Default)
        {
            this.ViewModel = new SettingsDialogVM(window, this, settings, activeTabType);

            this.InitializeComponent();
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }
    }
}
