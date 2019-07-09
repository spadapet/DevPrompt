using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System.Windows;

namespace DevPrompt.UI.Settings
{
    internal partial class SettingsImportDialog : Window
    {
        public SettingsImportDialogVM ViewModel { get; }

        public SettingsImportDialog(AppSettings settings)
        {
            this.ViewModel = new SettingsImportDialogVM(settings);

            this.InitializeComponent();
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }
    }
}
