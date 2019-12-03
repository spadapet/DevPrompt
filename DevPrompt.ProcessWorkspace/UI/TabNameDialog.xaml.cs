using DevPrompt.ProcessWorkspace.UI.ViewModels;
using System.Windows;

namespace DevPrompt.ProcessWorkspace.UI
{
    internal sealed partial class TabNameDialog : Window
    {
        public TabNameDialogVM ViewModel { get; }

        public TabNameDialog(TabNameDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            this.editControl.Focus();
            this.editControl.SelectAll();
        }
    }
}
