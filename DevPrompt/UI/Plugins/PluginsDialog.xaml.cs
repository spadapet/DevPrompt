using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System;
using System.Windows;

namespace DevPrompt.UI.Plugins
{
    internal partial class PluginsDialog : Window, IDisposable
    {
        public PluginsDialogVM ViewModel { get; }

        public PluginsDialog(MainWindow window, AppSettings settings, PluginsTabType activeTabType = PluginsTabType.Default)
        {
            this.Owner = window;
            this.ViewModel = new PluginsDialogVM(window, this, settings, activeTabType);

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
