using DevOps.Avatars;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestVM : PropertyNotifier, IPullRequestVM, IAvatarSite
    {
        private GitPullRequest pr;
        private IAvatarProvider avatarProvider;
        private ImageSource avatarImageSource;

        public PullRequestVM(GitPullRequest pr, IAvatarProvider avatarProvider)
        {
            this.pr = pr;
            this.avatarProvider = avatarProvider;
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

        public Uri AvatarLink
        {
            get
            {
                IReadOnlyDictionary<string, object> links = this.pr.CreatedBy?.Links?.Links;

                if (links != null &&
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
                    Uri avatarUri = this.AvatarLink;
                    if (avatarUri != null)
                    {
                        this.avatarProvider.ProvideAvatar(Guid.Empty, this);
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
    }
}