using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestPageVM : PropertyNotifier, IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly PullRequestTabVM tab;
        private readonly GitHttpClient client;
        private Task<List<GitPullRequest>> activeTask;
        private ObservableCollection<PullRequestVM> pullRequests;

        public PullRequestPageVM(PullRequestTabVM tab, GitHttpClient client)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.tab = tab;
            this.client = client;
            this.pullRequests = new ObservableCollection<PullRequestVM>();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.client.Dispose();
        }

        public IList<PullRequestVM> PullRequests
        {
            get
            {
                return this.pullRequests;
            }
        }

        public IMainWindowVM Window
        {
            get
            {
                return this.tab.Window;
            }
        }

        public async Task OnLoaded()
        {
            if (this.activeTask == null)
            {
                GitPullRequestSearchCriteria searchCriteria = new GitPullRequestSearchCriteria()
                {
                    Status = PullRequestStatus.Active,
                };

                List<GitPullRequest> prs = null;

                using (this.Window.BeginLoading())
                {
                    try
                    {
                        this.activeTask = this.client.GetPullRequestsByProjectAsync("DevDiv", searchCriteria, cancellationToken: this.cancellationTokenSource.Token);
                        prs = await this.activeTask;
                    }
                    catch (Exception ex)
                    {
                        this.Window.SetError(ex);
                        prs = null;
                    }
                    finally
                    {
                        this.activeTask = null;
                    }
                }

                if (prs != null)
                {
                    this.pullRequests.Clear();

                    foreach (GitPullRequest pr in prs)
                    {
                        this.pullRequests.Add(new PullRequestVM(pr));
                    }
                }
            }
        }

        public void OnUnloaded()
        {
        }
    }
}
