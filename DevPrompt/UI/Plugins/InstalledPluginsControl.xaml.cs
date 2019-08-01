using DevPrompt.UI.ViewModels;
using System.Windows.Controls;

namespace DevPrompt.UI.Plugins
{
    internal partial class InstalledPluginsControl : UserControl
    {
        public PluginsDialogVM ViewModel { get; }

        public InstalledPluginsControl(PluginsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }
    }
}
