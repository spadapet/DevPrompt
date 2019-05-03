using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DevOps.UI.ViewModels
{
    internal class LoginPageVM : PropertyNotifier, IDisposable
    {
        private string organizationName;
        private string personalAccessToken;
        private readonly DelegateCommand okCommand;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly PullRequestTabVM tab;

        public LoginPageVM(PullRequestTabVM tab)
        {
            this.organizationName = "devdiv";
            this.okCommand = new DelegateCommand(async () => await this.OnOk(), this.OkCommandEnabled);
            this.cancellationTokenSource = new CancellationTokenSource();
            this.tab = tab;
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
        }

        public IMainWindowVM Window
        {
            get
            {
                return this.tab.Window;
            }
        }

        public string OrganizationName
        {
            get
            {
                return this.organizationName ?? string.Empty;
            }

            set
            {
                if (this.SetPropertyValue(ref this.organizationName, value))
                {
                    this.okCommand.UpdateCanExecute();
                }
            }
        }

        public string PersonalAccessToken
        {
            get
            {
                return this.personalAccessToken ?? string.Empty;
            }

            set
            {
                if (this.SetPropertyValue(ref this.personalAccessToken, value))
                {
                    this.okCommand.UpdateCanExecute();
                }
            }
        }

        public ICommand OkCommand
        {
            get
            {
                return this.okCommand;
            }
        }

        private bool OkCommandEnabled()
        {
            return !string.IsNullOrWhiteSpace(this.organizationName) && !string.IsNullOrWhiteSpace(this.personalAccessToken);
        }

        private async Task OnOk()
        {
            using (this.Window.BeginLoading())
            {
                try
                {
                    VssConnection connection = await Globals.Instance.GetVssConnection(this.OrganizationName.Trim(), this.PersonalAccessToken.Trim());
                    GitHttpClient client = await connection.GetClientAsync<GitHttpClient>(this.cancellationTokenSource.Token);

                    this.tab.ViewElement = new PullRequestPage(this.tab, client);
                }
                catch (Exception ex)
                {
                    this.tab.Window.SetError(ex);
                }
            }
        }
    }
}
