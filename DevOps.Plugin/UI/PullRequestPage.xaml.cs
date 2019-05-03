using DevOps.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DevOps.UI
{
    internal partial class PullRequestPage : UserControl, IDisposable
    {
        public PullRequestPageVM ViewModel { get; }

        public PullRequestPage(PullRequestPageVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }

        public void Dispose()
        {
            this.ViewModel.Dispose();
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            await this.ViewModel.OnLoaded();
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            this.ViewModel.OnUnloaded();
        }
    }
}
