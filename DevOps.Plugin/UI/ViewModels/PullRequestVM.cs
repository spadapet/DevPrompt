using DevOps.Avatars;
using DevPrompt.Api;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestVM : PropertyNotifier, IPullRequestVM, IAvatarSite
    {
        private Uri baseUri;
        private GitPullRequest pr;
        private IAvatarProvider avatarProvider;
        private ImageSource avatarImageSource;
        private IWindow window;

        public PullRequestVM(Uri baseUri, GitPullRequest pr, IAvatarProvider avatarProvider, IWindow window)
        {
            this.baseUri = baseUri;
            this.pr = pr;
            this.avatarProvider = avatarProvider;
            this.window = window;
        }

        public bool IsDraft => this.pr.IsDraft == true;
        public string Id => this.pr.PullRequestId.ToString();
        public string Title => this.pr.Title;
        public string Author => this.pr.CreatedBy?.DisplayName ?? string.Empty;
        public string Status => this.pr.Status.ToString();
        public string SourceRefName => this.pr.SourceRefName;
        public string TargetRefName => this.pr.TargetRefName;
        public DateTime CreationDate => this.pr.CreationDate;

        public GitPullRequest GitPullRequest
        {
            get => this.pr;
            set
            {
                if (this.SetPropertyValue(ref this.pr, value, name: null))
                {
                    this.OnPropertiesChanged();
                }
            }
        }

        public Uri BaseAddress
        {
            get => this.baseUri;
            set
            {
                if (this.SetPropertyValue(ref this.baseUri, value))
                {
                    this.OnPropertyChanged(nameof(this.WebLink));
                    this.OnPropertyChanged(nameof(this.CodeFlowLink));
                }
            }
        }

        public Uri WebLink => new Uri($@"{this.baseUri}{this.pr.Repository.ProjectReference.Name}/_git/{this.pr.Repository.Name}/pullrequest/{this.pr.PullRequestId}?_a=overview");

        public Uri CodeFlowLink => new Uri($@"codeflow://open/?server={Uri.EscapeUriString(this.baseUri.ToString())}&project={this.pr.Repository.ProjectReference.Name}&repo={this.pr.Repository.Name}&pullRequest={this.pr.PullRequestId}&alert=true");

        public Uri AvatarLink
        {
            get
            {
                if (this.pr.CreatedBy?.Links?.Links is IReadOnlyDictionary<string, object> links &&
                    links.TryGetValue("avatar", out object avatarValue) &&
                    avatarValue is ReferenceLink avatarLink &&
                    !string.IsNullOrEmpty(avatarLink.Href) &&
                    Uri.TryCreate(avatarLink.Href, UriKind.Absolute, out Uri avatarUri))
                {
                    return avatarUri;
                }

                return null;
            }
        }

        public ImageSource AvatarImageSource
        {
            get
            {
                if (this.avatarImageSource == null)
                {
                    if (this.AvatarLink is Uri avatarUri)
                    {
                        this.avatarProvider.ProvideAvatar(avatarUri, this);
                    }
                }

                return this.avatarImageSource;
            }

            set
            {
                this.SetPropertyValue(ref this.avatarImageSource, value);
            }
        }

        public ICommand WebLinkCommand => new DelegateCommand(p =>
        {
            if (p is Uri uri)
            {
                this.window.RunExternalProcess(uri.ToString());
            }
        });
    }
}
