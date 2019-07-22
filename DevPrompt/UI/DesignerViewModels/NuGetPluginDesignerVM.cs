using DevPrompt.UI.ViewModels;

namespace DevPrompt.UI.DesignerViewModels
{
    internal class NuGetPluginDesignerVM : IPluginVM
    {
        public string Title => "Plugin Title";
        public string Description => "This is my plugin description. It is cool.";
        public string Version => "1.0.1";
        public string ProjectUrl => "http://www.microsoft.com";
        public string Authors => "Bill Gates";

        public bool IsInstalled => true;
        public string InstalledVersion => "1.0.0";

        public NuGetPluginDesignerVM()
        {
        }
    }
}
