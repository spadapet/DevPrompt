using DevOps.UI.ViewModels;
using System;
using System.Windows.Controls;

namespace DevOps.UI
{
    internal partial class LoginPage : UserControl, IDisposable
    {
        public PullRequestTab Tab { get; }
        public LoginPageVM ViewModel { get; }

        public LoginPage(PullRequestTab tab)
        {
            this.Tab = tab;
            this.ViewModel = new LoginPageVM(tab);

            this.InitializeComponent();
        }

        public void Dispose()
        {
            this.ViewModel.Dispose();
        }
    }
}
