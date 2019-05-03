﻿using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.FileContainer.Client;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Profile.Client;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.IO;
using System.Net.Http;
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
        private readonly PullRequestTabVM tab;

        /// <summary>
        /// Sample data for the XAML designer
        /// </summary>
        public LoginPageVM()
        {
            this.organizationName = "devdiv";
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public LoginPageVM(PullRequestTabVM tab)
        {
            this.organizationName = "devdiv";
            this.projectName = "devdiv";
            this.cancellationTokenSource = new CancellationTokenSource();
            this.okCommand = new DelegateCommand(async () => await this.OnOk(), this.OkCommandEnabled);
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
            ProfileHttpClient profileClient = await connection.GetClientAsync<ProfileHttpClient>(this.cancellationTokenSource.Token);
            ProjectHttpClient projectClient = await connection.GetClientAsync<ProjectHttpClient>(this.cancellationTokenSource.Token);
            TeamProject project = await projectClient.GetProject(this.ProjectName);

            PullRequestPageVM vm = new PullRequestPageVM(this.tab, gitClient, profileClient, new TeamProject[] { project });
            this.tab.ViewElement = new PullRequestPage(vm);
        }
    }
}