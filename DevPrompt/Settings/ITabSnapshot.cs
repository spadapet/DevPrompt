using DevPrompt.UI.ViewModels;
using System;

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
