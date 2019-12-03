namespace DevPrompt.Api
{
    /// <summary>
    /// Listens to global app events
    /// </summary>
    public interface IAppListener
    {
        void OnStartup(IApp app);
        void OnOpened(IApp app, IWindow window);
        void OnClosing(IApp app, IWindow window);
        void OnExit(IApp app);
    }
}
