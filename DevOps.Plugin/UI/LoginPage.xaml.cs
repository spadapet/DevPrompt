using DevOps.UI.ViewModels;
using DevOps.Utility;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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
                List<Account> accounts;
                VssAadCredential vssAadCred;

                using (this.ViewModel.Window.BeginLoading(() => this.cancellationTokenSource.Cancel()))
                {
                    vssAadCred = await AzureDevOpsClient.CreateVssAadCredentials();
                    accounts = await AzureDevOpsClient.GetAccountsAsync(vssAadCred, this.cancellationTokenSource.Token);
                }

                PullRequestPageVM vm = new PullRequestPageVM(this.Tab, accounts, vssAadCred);
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
