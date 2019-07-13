using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System;
using System.Windows;

namespace DevPrompt.UI.Settings
{
    internal partial class SettingsDialog : Window, IDisposable
    {
        public SettingsDialogVM ViewModel { get; }

        public SettingsDialog(MainWindow window, AppSettings settings, SettingsTabType activeTabType = SettingsTabType.Default)
        {
            this.Owner = window;
            this.ViewModel = new SettingsDialogVM(window, this, settings, activeTabType);

            this.InitializeComponent();
        }

        public void Dispose()
        {
            this.ViewModel.Dispose();
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }
    }
}
