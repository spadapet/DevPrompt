using System;
using System.Windows.Input;
using System.Windows.Media;

namespace DevOps.UI.ViewModels
{
    internal interface IPullRequestVM
    {
        bool IsDraft { get; }
        string Status { get; }
        string SourceRefName { get; }
        string TargetRefName { get; }
        string Id { get; }
        string Title { get; }
        string Author { get; }
        Uri WebLink { get; }
        Uri CodeFlowLink { get; }
        Uri AvatarLink { get; }
        ImageSource AvatarImageSource { get; }
        DateTime CreationDate { get; }

        ICommand WebLinkCommand { get; }
    }
}
