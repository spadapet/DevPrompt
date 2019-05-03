using System;
using System.Windows.Media;

namespace DevOps.UI.ViewModels
{
    internal interface IPullRequestVM
    {
        string Id { get; }
        string Title { get; }
        string Author { get; }
        Uri AvatarLink { get; }
        ImageSource AvatarImageSource { get; }
        DateTime CreationDate { get; }
        bool IsDraft { get; }
        string Status { get; }
        string SourceRefName { get; }
        string TargetRefName { get; }
    }
}
