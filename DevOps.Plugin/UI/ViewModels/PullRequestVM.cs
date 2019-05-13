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

        public GitPullRequest GitPullRequest
        {
            get
            {
                return this.pr;
            }

            set
            {
                this.SetPropertyValue(ref this.pr, value, name: null);
            }
        }

        public Uri WebLink
        {
            get
            {
                return new Uri($@"{this.baseUri}{this.pr.Repository.ProjectReference.Name}/_git/{this.pr.Repository.Name}/pullrequest/{this.pr.PullRequestId}?_a=overview");
            }
        }

        public Uri CodeFlowLink
        {
            get
            {
                return new Uri($@"codeflow://open/?server={Uri.EscapeUriString(this.baseUri.ToString())}&project={this.pr.Repository.ProjectReference.Name}&repo={this.pr.Repository.Name}&pullRequest={this.pr.PullRequestId}&alert=true");
            }
        }

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

        public string Id
        {
            get
            {
                return this.pr.PullRequestId.ToString();
            }
        }

        public string Title
        {
            get
            {
                return this.pr.Title;
            }
        }

        public string Author
        {
            get
            {
                return this.pr.CreatedBy?.DisplayName ?? string.Empty;
            }
        }

        public DateTime CreationDate
        {
            get
            {
                return this.pr.CreationDate;
            }
        }

        public bool IsDraft
        {
            get
            {
                return this.pr.IsDraft == true;
            }
        }

        public string Status
        {
            get
            {
                return this.pr.Status.ToString();
            }
        }

        public string SourceRefName
        {
            get
            {
                return this.pr.SourceRefName;
            }
        }

        public string TargetRefName
        {
            get
            {
                return this.pr.TargetRefName;
            }
        }

        public ICommand WebLinkCommand
        {
            get
            {
                return new DelegateCommand(p =>
                {
                    if (p is Uri uri)
                    {
                        this.window.RunExternalProcess(uri.ToString());
                    }
                });
            }
        }
    }
}
