using DevOps.UI.ViewModels;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DevOps.UI
{
    internal partial class PullRequestPage : UserControl, IDisposable
    {
        public PullRequestTabVM Tab { get; }
        public PullRequestPageVM ViewModel { get; }

        public PullRequestPage(PullRequestTabVM tab, GitHttpClient client)
        {
            this.Tab = tab;
            this.ViewModel = new PullRequestPageVM(tab.Window, client);

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
