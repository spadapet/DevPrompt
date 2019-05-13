using DevOps.Avatars;
using DevPrompt.Api;
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
        public PullRequestTab Tab { get; }
        private CancellationTokenSource cancellationTokenSource;
        private readonly GitHttpClient gitClient;
        private readonly HttpClient avatarHttpClient;
        private List<TeamProject> projects;
        private Task activeTask;
        private ObservableCollection<PullRequestVM> pullRequests;
        private Dictionary<Uri, List<IAvatarSite>> pendingAvatars;
        private Dictionary<Uri, ImageSource> avatars;
        private bool disposed;

        public PullRequestPageVM(
            PullRequestTab tab,
            GitHttpClient gitClient,
            HttpClient avatarHttpClient,
            IEnumerable<TeamProject> projects)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.Tab = tab;
            this.gitClient = gitClient;
            this.avatarHttpClient = avatarHttpClient;
            this.projects = projects.ToList();
            this.pullRequests = new ObservableCollection<PullRequestVM>();
            this.pendingAvatars = new Dictionary<Uri, List<IAvatarSite>>();
            this.avatars = new Dictionary<Uri, ImageSource>();
        }

        public void Dispose()
        {
            this.disposed = true;

            this.Cancel(disposing: true);
            this.avatarHttpClient.Dispose();
        }

        public IList<PullRequestVM> PullRequests
        {
            get
            {
                return this.pullRequests;
            }
        }

        public IWindow Window
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
            if (!this.disposed)
            {
                this.Cancel(disposing: false);
            }
        }

        private void Cancel(bool disposing)
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();

            if (!this.disposed)
            {
                this.cancellationTokenSource = new CancellationTokenSource();
            }
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
            // Clear failed avatar downloads
            foreach (KeyValuePair<Uri, ImageSource> pair in this.avatars.ToArray())
            {
                if (pair.Value == null)
                {
                    this.avatars.Remove(pair.Key);
                }
            }

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
            Uri baseUri = this.gitClient.BaseAddress;

            for (int i = 0; i < newPullRequests.Count; i++)
            {
                if (this.pullRequests.Count > i)
                {
                    this.pullRequests[i].GitPullRequest = newPullRequests[i];
                }
                else
                {
                    this.pullRequests.Add(new PullRequestVM(baseUri, newPullRequests[i], this, this.Window));
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
                    HttpResponseMessage response = await this.avatarHttpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, this.cancellationTokenSource.Token);
                    response = response.EnsureSuccessStatusCode();

                    Stream stream = await response.Content.ReadAsStreamAsync();
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
