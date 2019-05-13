namespace DevPrompt.Api
{
    /// <summary>
    /// Export this from a plugin to know when processes are opened and closed
    /// </summary>
    public interface IProcessListener
    {
        void OnProcessOpening(IProcess process, bool activate, string path);
        void OnProcessClosing(IProcess process);
    }
}
