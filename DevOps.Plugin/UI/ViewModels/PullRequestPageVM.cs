using DevOps.Avatars;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestPageVM : PropertyNotifier, IAvatarProvider, IDisposable
    {
        public PullRequestTabVM Tab { get; }
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly GitHttpClient gitClient;
        private readonly HttpClient httpClient;
        private List<TeamProject> projects;
        private Task activeTask;
        private ObservableCollection<PullRequestVM> pullRequests;
        private Dictionary<Uri, List<IAvatarSite>> pendingAvatars;
        private Dictionary<Uri, ImageSource> avatars;

        public PullRequestPageVM(
            PullRequestTabVM tab,
            GitHttpClient gitClient,
            HttpClient httpClient,
            IEnumerable<TeamProject> projects)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.Tab = tab;
            this.gitClient = gitClient;
            this.httpClient = httpClient;
            this.projects = projects.ToList();
            this.pullRequests = new ObservableCollection<PullRequestVM>();
            this.pendingAvatars = new Dictionary<Uri, List<IAvatarSite>>();
            this.avatars = new Dictionary<Uri, ImageSource>();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.httpClient.Dispose();
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

        async void IAvatarProvider.ProvideAvatar(Uri uri, IAvatarSite site)
        {
            if (this.avatars.TryGetValue(uri, out ImageSource image))
            {
                site.AvatarImageSource = image;
                return;
            }

            if (!this.pendingAvatars.TryGetValue(uri, out List<IAvatarSite> pendingSites))
            {
                pendingSites = new List<IAvatarSite>();
                this.pendingAvatars[uri] = pendingSites;
            }

            if (!pendingSites.Contains(site))
            {
                pendingSites.Add(site);

                try
                {
                    Stream stream = await this.httpClient.GetStreamAsync(uri);
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    image = bitmap;
                    this.avatars[uri] = image;
                }
                catch
                {
                    // oh well, the image won't show
                    this.avatars[uri] = null;
                }
                finally
                {
                    site.AvatarImageSource = image;

                    if (pendingSites.Remove(site) && pendingSites.Count == 0)
                    {
                        this.pendingAvatars.Remove(uri);
                    }
                }
            }
        }
    }
}
