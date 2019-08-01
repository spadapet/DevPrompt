using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DevPrompt.UI.ViewModels
{
    [Flags]
    internal enum PluginState
    {
        None = 0x00,
        Installed = 0x01,
        UpdateAvailable = 0x02,
        Busy = 0x04,
    }

    internal interface IPluginVM : IDisposable
    {
        string Title { get; }
        string Description { get; }
        string Summary { get; }
        string InstalledVersion { get; }
        string LatestVersion { get; }
        string Authors { get; }
        Uri ProjectUrl { get; }
        ImageSource Icon { get; }
        PluginState State { get; }

        Task Install(CancellationToken cancelToken);
        Task Uninstall(CancellationToken cancelToken);
    }
}
