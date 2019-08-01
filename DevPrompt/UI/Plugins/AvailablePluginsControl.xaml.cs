using DevPrompt.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace DevPrompt.UI.Plugins
{
    internal partial class AvailablePluginsControl : UserControl
    {
        public PluginsDialogVM ViewModel { get; }

        public AvailablePluginsControl(PluginsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }

        private void OnHyperlink(object sender, RequestNavigateEventArgs args)
        {
            if (sender is Hyperlink link && link.NavigateUri != null)
            {
                this.ViewModel.Window.ViewModel.RunExternalProcess(link.NavigateUri.ToString());
            }
        }
    }
}
