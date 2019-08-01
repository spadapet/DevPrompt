using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DevPrompt.UI.ViewModels
{
    internal interface IPluginVM
    {
        string Title { get; }
        string Description { get; }
        string Summary { get; }
        string LatestVersion { get; }
        Uri ProjectUrl { get; }
        string Authors { get; }
        ImageSource Icon { get; }

        bool IsInstalled { get; }
        bool IsInstalling { get; }
        string InstalledVersion { get; }
        Task Install(CancellationToken cancelToken);
        Task Uninstall(CancellationToken cancelToken);
    }
}
