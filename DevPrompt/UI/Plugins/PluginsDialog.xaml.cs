using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System.Windows;

namespace DevPrompt.UI.Plugins
{
    internal partial class PluginsDialog : Window
    {
        public PluginsDialogVM ViewModel { get; }

        public PluginsDialog(MainWindow window, AppSettings settings, PluginsTabType activeTabType = PluginsTabType.Default)
        {
            this.Owner = window;
            this.ViewModel = new PluginsDialogVM(window, this, settings, activeTabType);

            this.InitializeComponent();
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }
    }
}
