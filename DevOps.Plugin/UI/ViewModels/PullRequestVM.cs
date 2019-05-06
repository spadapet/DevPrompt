using DevOps.Avatars;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestVM : PropertyNotifier, IPullRequestVM, IAvatarSite
    {
        private GitPullRequest pr;
        private IAvatarProvider avatarProvider;
        private ImageSource avatarImageSource;
        private IMainWindowVM window;

        public PullRequestVM(GitPullRequest pr, IAvatarProvider avatarProvider, IMainWindowVM window)
        {
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
                return new Uri("http://www.peterspada.com");
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

        private static string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder stringBuilder = new StringBuilder(data.Length);

                for (int i = 0; i < data.Length; i++)
                {
                    stringBuilder.Append(data[i].ToString("x2"));
                }

                return stringBuilder.ToString();
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
                        //avatarUri = new Uri($"http://gravatar.com/avatar/{PullRequestVM.GetMd5Hash("spadapet@hotmail.com")}?s=32");
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
                        this.window.StartProcess(uri.ToString());
                    }
                });
            }
        }
    }
}
