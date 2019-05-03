using DevPrompt.Utility;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestVM : PropertyNotifier
    {
        private GitPullRequest pr;

        public PullRequestVM(GitPullRequest pr)
        {
            this.pr = pr;
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
                return this.pr.CreatedBy.DisplayName;
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
