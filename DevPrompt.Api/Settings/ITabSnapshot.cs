using System;

namespace DevPrompt.Api
{
    /// <summary>
    /// Helps restore a tab between sessions
    /// </summary>
    public interface ITabSnapshot
    {
        Guid Id { get; }
        string Name { get; }
        string Tooltip { get; }

        ITabSnapshot Clone();
        ITab Restore(IWindow window, ITabWorkspace workspace);
    }
}
