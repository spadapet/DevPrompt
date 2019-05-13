namespace DevPrompt.Interop
{
    /// <summary>
    /// Forwrds certain IAppHost calls to internal listeners (ImportMany by the app)
    /// </summary>
    internal interface IProcessListener
    {
        void OnProcessOpening(IProcess process, bool activate, string path);
        void OnProcessClosing(IProcess process);
        void OnProcessEnvChanged(IProcess process, string env);
        void OnProcessTitleChanged(IProcess process, string title);
    }
}
