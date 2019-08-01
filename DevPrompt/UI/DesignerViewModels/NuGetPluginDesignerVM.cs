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
        public string LatestVersion => "1.0.1";
        public Uri ProjectUrl => new Uri("http://www.microsoft.com");
        public string Authors => "Bill Gates";
        public ImageSource Icon => null;

        public bool IsInstalled => true;
        public bool IsInstalling => false;
        public string InstalledVersion => "1.0.0";
        public Task Install(CancellationToken cancelToken) => Task.CompletedTask;
        public Task Uninstall(CancellationToken cancelToken) => Task.CompletedTask;
    }
}
