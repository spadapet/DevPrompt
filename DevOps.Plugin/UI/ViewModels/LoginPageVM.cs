using DevPrompt.Api;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DevOps.UI.ViewModels
{
    internal class LoginPageVM : PropertyNotifier, IDisposable
    {
        private string organizationName;
        private string projectName;
        private string personalAccessToken;
        private readonly DelegateCommand okCommand;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly PullRequestTab tab;

        /// <summary>
        /// Sample data for the XAML designer
        /// </summary>
        public LoginPageVM()
        {
            this.organizationName = "devdiv";
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public LoginPageVM(PullRequestTab tab)
        {
            this.organizationName = "devdiv";
            this.projectName = "devdiv";
            this.cancellationTokenSource = new CancellationTokenSource();
            this.okCommand = new DelegateCommand(async () => await this.OnOk(), this.OkCommandEnabled);
            this.tab = tab;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("/pat=", StringComparison.Ordinal))
                {
                    this.personalAccessToken = arg.Substring(5);
                }
            }
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
        }

        public IWindow Window
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

        public string ProjectName
        {
            get
            {
                return this.projectName ?? string.Empty;
            }

            set
            {
                if (this.SetPropertyValue(ref this.projectName, value))
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
                    this.OnPropertyChanged(nameof(this.AuthenticationBase64));
                    this.okCommand.UpdateCanExecute();
                }
            }
        }

        public string AuthenticationBase64
        {
            get
            {
                return Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + this.PersonalAccessToken));
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
            return !string.IsNullOrWhiteSpace(this.organizationName) &&
                !string.IsNullOrWhiteSpace(this.projectName) &&
                !string.IsNullOrWhiteSpace(this.personalAccessToken);
        }

        private async Task OnOk()
        {
            using (this.Window.BeginLoading())
            {
                try
                {
                    await this.LoadPullRequestPage();
                }
                catch (Exception ex)
                {
                    this.Window.SetError(ex);
                }
            }
        }

        private async Task LoadPullRequestPage()
        {
            VssConnection connection = await Globals.Instance.GetVssConnection(this.OrganizationName.Trim(), this.PersonalAccessToken.Trim(), this.cancellationTokenSource.Token);
            GitHttpClient gitClient = await connection.GetClientAsync<GitHttpClient>(this.cancellationTokenSource.Token);
            ProjectHttpClient projectClient = await connection.GetClientAsync<ProjectHttpClient>(this.cancellationTokenSource.Token);
            TeamProject project = await projectClient.GetProject(this.ProjectName);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", this.AuthenticationBase64);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

            PullRequestPageVM vm = new PullRequestPageVM(this.tab, gitClient, httpClient, new TeamProject[] { project });
            this.tab.ViewElement = new PullRequestPage(vm);
        }
    }
}
