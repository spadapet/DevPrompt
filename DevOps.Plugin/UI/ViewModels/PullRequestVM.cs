using DevPrompt.Utility;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace DevOps.UI.ViewModels
{
    internal class PullRequestVM : PropertyNotifier
    {
        private GitPullRequest pr;

        public PullRequestVM(GitPullRequest pr)
        {
            this.pr = pr;
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
    }
}
