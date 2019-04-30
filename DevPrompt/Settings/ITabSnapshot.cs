using DevPrompt.UI.ViewModels;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Helps restore a tab between sessions
    /// </summary>
    public interface ITabSnapshot
    {
        ITabSnapshot Clone();
        ITabVM Restore(IMainWindowVM window);
    }
}
