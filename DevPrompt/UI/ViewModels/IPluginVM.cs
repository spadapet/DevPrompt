namespace DevPrompt.UI.ViewModels
{
    internal interface IPluginVM
    {
        string Title { get; }
        string Description { get; }
        string Version { get; }
        string ProjectUrl { get; }
        string Authors { get; }
        bool IsInstalled { get; }
        string InstalledVersion { get; }
    }
}
