using DevOps.Avatars;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Profile.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestPageVM : PropertyNotifier, IAvatarProvider, IDisposable
    {
        public PullRequestTabVM Tab { get; }
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly GitHttpClient gitClient;
        private readonly ProfileHttpClient profileClient;
        private List<TeamProject> projects;
        private Task activeTask;
        private ObservableCollection<PullRequestVM> pullRequests;
        private Dictionary<Guid, List<IAvatarSite>> pendingAvatars;

        public PullRequestPageVM(
            PullRequestTabVM tab,
            GitHttpClient gitClient,
            ProfileHttpClient profileClient,
            IEnumerable<TeamProject> projects)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.Tab = tab;
            this.gitClient = gitClient;
            this.profileClient = profileClient;
            this.projects = projects.ToList();
            this.pullRequests = new ObservableCollection<PullRequestVM>();
            this.pendingAvatars = new Dictionary<Guid, List<IAvatarSite>>();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.gitClient.Dispose();
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
                return this.Tab.Window;
            }
        }

        public async Task OnLoaded()
        {
            await this.Refresh();
        }

        public void OnUnloaded()
        {
        }

        public async Task Refresh()
        {
            if (this.activeTask == null)
            {
                using (this.Window.BeginLoading())
                {
                    try
                    {
                        this.activeTask = this.InternalRefresh();
                        await this.activeTask;
                    }
                    catch (Exception ex)
                    {
                        this.Window.SetError(ex);
                    }
                    finally
                    {
                        this.activeTask = null;
                    }
                }
            }
        }

        private async Task InternalRefresh()
        {
            GitPullRequestSearchCriteria searchCriteria = new GitPullRequestSearchCriteria()
            {
                Status = PullRequestStatus.Active,
            };

            List<GitPullRequest> newPullRequests = new List<GitPullRequest>();

            foreach (TeamProject project in this.projects)
            {
                newPullRequests.AddRange(await this.gitClient.GetPullRequestsByProjectAsync(project.Id, searchCriteria, cancellationToken: this.cancellationTokenSource.Token));
            }

            this.UpdatePullRequests(newPullRequests);
        }

        private void UpdatePullRequests(IReadOnlyList<GitPullRequest> newPullRequests)
        {
            for (int i = 0; i < newPullRequests.Count; i++)
            {
                if (this.pullRequests.Count > i)
                {
                    this.pullRequests[i].GitPullRequest = newPullRequests[i];
                }
                else
                {
                    this.pullRequests.Add(new PullRequestVM(newPullRequests[i], this));
                }
            }

            while (this.pullRequests.Count > newPullRequests.Count)
            {
                this.pullRequests.RemoveAt(this.pullRequests.Count - 1);
            }
        }

        void IAvatarProvider.ProvideAvatar(Guid id, IAvatarSite site)
        {
            List<IAvatarSite> pendingSites;
            if (this.pendingAvatars.TryGetValue(id, out pendingSites))
            {
                if (pendingSites.Contains(site))
                {
                    return;
                }
            }
            else
            {
                pendingSites = new List<IAvatarSite>();
                this.pendingAvatars[id] = pendingSites;
            }

            pendingSites.Add(site);

            //this.profileClient.GetAvatarAsync(
        }
    }
}
