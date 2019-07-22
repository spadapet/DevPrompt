using DevPrompt.Api;
using DevPrompt.Settings;

namespace DevPrompt.UI.ViewModels
{
    internal class NuGetPluginVM : PropertyNotifier, IPluginVM
    {
        public string Title => this.settings.Title;
        public string Description => this.settings.Description;
        public string Version => this.settings.Version;
        public string ProjectUrl => this.settings.ProjectUrl;
        public string Authors => this.settings.Authors;

        public bool IsInstalled => !string.IsNullOrEmpty(this.settings.Path);
        public string InstalledVersion => this.settings.Version;

        private NuGetPluginSettings settings;

        public NuGetPluginVM(NuGetPluginSettings settings)
        {
            this.settings = settings;
        }
    }
}
