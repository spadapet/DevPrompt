using System.Windows.Controls;
using DevPrompt.UI.ViewModels;

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
    }
}
