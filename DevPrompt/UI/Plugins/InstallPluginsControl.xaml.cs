using DevPrompt.UI.ViewModels;
using System.Windows.Controls;

namespace DevPrompt.UI.Plugins
{
    internal partial class InstallPluginsControl : UserControl
    {
        public PluginsDialogVM ViewModel { get; }

        public InstallPluginsControl(PluginsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }
    }
}
