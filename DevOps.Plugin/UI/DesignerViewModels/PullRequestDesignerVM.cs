using DevOps.UI.ViewModels;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace DevOps.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class PullRequestDesignerVM : IPullRequestVM
    {
        public string Id { get; set; } = "123456";
        public string Title { get; set; } = "Sample Title";
        public string Author { get; set; } = "Author Name";
        public Uri WebLink { get; set; }
        public Uri CodeFlowLink { get; set; }
        public Uri AvatarLink { get; set; }
        public ImageSource AvatarImageSource { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public bool IsDraft { get; set; } = false;
        public string Status { get; set; } = "Active";
        public string SourceRefName { get; set; } = "branch/source";
        public string TargetRefName { get; set; } = "branch/target";
        public ICommand WebLinkCommand { get; }
    }
}
