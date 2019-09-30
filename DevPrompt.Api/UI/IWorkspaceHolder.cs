using System;

namespace DevPrompt.Api
{
    /// <summary>
    /// This holds a workspace that's alrady been added to the window
    /// </summary>
    public interface IWorkspaceHolder
    {
        Guid Id { get; }
        ActiveState ActiveState { get; set; }
        bool CreatedWorkspace { get; }
        IWorkspace Workspace { get; }
    }
}
