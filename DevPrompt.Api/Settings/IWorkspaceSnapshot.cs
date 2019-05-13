namespace DevPrompt.Api
{
    /// <summary>
    /// Helps restore a workspace between sessions
    /// </summary>
    public interface IWorkspaceSnapshot
    {
        IWorkspaceSnapshot Clone();
        IWorkspace Restore(IWindow window);
    }
}
