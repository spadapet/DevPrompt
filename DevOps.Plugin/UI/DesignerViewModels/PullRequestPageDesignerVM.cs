using DevOps.UI.ViewModels;
using System.Collections.Generic;

namespace DevOps.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class PullRequestPageDesignerVM
    {
        public PullRequestPageDesignerVM()
        {
        }

        public IList<IPullRequestVM> PullRequests
        {
            get
            {
                return new PullRequestDesignerVM[]
                    {
                        new PullRequestDesignerVM(),
                        new PullRequestDesignerVM(),
                        new PullRequestDesignerVM(),
                        new PullRequestDesignerVM(),
                    };
            }
        }
    }
}
