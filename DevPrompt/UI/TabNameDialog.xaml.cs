using DevPrompt.UI.ViewModels;
using System.Windows;

namespace DevPrompt.UI
{
    internal partial class TabNameDialog : Window
    {
        public TabNameDialogVM ViewModel { get; }

        public TabNameDialog(string tabName)
        {
            this.ViewModel = new TabNameDialogVM(tabName);

            this.InitializeComponent();
        }

        public string TabName
        {
            get
            {
                return this.ViewModel.Name;
            }
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
