using System;

namespace DevPrompt.Api
{
    /// <summary>
    /// Adds a workspace to the main window
    /// </summary>
    public interface IWorkspaceProvider
    {
        Guid WorkspaceId { get; }
        string WorkspaceName { get; }
        string WorkspaceTooltip { get; }

        IWorkspace CreateWorkspace(IWindow window);
    }
}
