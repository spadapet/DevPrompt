using DevOps.UI.ViewModels;
using DevOps.Utility;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace DevOps.UI
{
    internal partial class LoginPage : UserControl
    {
        public PullRequestTab Tab { get; }
        public LoginPageVM ViewModel { get; }
        private CancellationTokenSource cancellationTokenSource;

        public LoginPage(PullRequestTab tab)
        {
            this.Tab = tab;
            this.ViewModel = new LoginPageVM(tab);

            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            try
            {
                AzureDevOpsUserContext devopsUserContext;

                using (this.ViewModel.Window.BeginLoading(() => this.cancellationTokenSource.Cancel()))
                {
                    devopsUserContext = await AzureDevOpsClient.GetUserContext();
                }

                PullRequestPageVM vm = new PullRequestPageVM(this.Tab, devopsUserContext);
                this.Tab.ViewElement = new PullRequestPage(vm);
            }
            catch (Exception ex)
            {
                this.ViewModel.DisplayText = "Error logging in:";
                this.ViewModel.InfoText = ex.ToString();
            }
            finally
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            this.cancellationTokenSource?.Cancel();
        }
    }
}
