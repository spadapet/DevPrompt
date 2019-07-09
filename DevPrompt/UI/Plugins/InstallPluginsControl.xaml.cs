using System.Windows.Controls;
using DevPrompt.UI.ViewModels;

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
