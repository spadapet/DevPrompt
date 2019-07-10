using DevPrompt.UI.ViewModels;
using System.Windows.Controls;

namespace DevPrompt.UI.Plugins
{
    internal partial class UpdatePluginsControl : UserControl
    {
        public PluginsDialogVM ViewModel { get; }

        public UpdatePluginsControl(PluginsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }
    }
}
