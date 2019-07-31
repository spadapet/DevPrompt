using DevPrompt.UI.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DevPrompt.UI.DesignerViewModels
{
    internal class NuGetPluginDesignerVM : IPluginVM
    {
        public string Title => "Plugin Title";
        public string Description => "This is my plugin description.";
        public string Summary => "Test summary, it's longer than a description. Explain the features in the plugin.";
        public string InstalledVersion => "1.0.0";
        public string LatestVersion => ((this.State & PluginState.UpdateAvailable) == PluginState.UpdateAvailable) ? "1.0.1" : this.InstalledVersion;
        public DateTime LatestVersionDate => DateTime.MinValue;
        public string Authors => "Bill Gates";
        public Uri ProjectUrl => new Uri("http://www.microsoft.com");
        public ImageSource Icon { get; }
        public PluginState State { get; }

        public Task Install(CancellationToken cancelToken) => Task.CompletedTask;
        public Task Uninstall(CancellationToken cancelToken) => Task.CompletedTask;

        public NuGetPluginDesignerVM(bool installed = false, bool updateAvailable = false, bool busy = false)
        {
            this.State =
                (installed ? PluginState.Installed : PluginState.None) |
                (updateAvailable ? PluginState.UpdateAvailable : PluginState.None) |
                (busy ? PluginState.Busy : PluginState.None);
        }

        public void Dispose()
        {
        }
    }
}
