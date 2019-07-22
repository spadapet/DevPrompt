using DevPrompt.Api;
using System;
using System.Composition;

namespace DevOps
{
    /// <summary>
    /// Stores global data for this plugin, only one will ever be created at a time.
    /// VSS connections are cached here so that they can be shared among views.
    /// </summary>
    [Export(typeof(IAppListener))]
    internal class Globals : IAppListener, IDisposable
    {
        public static Globals Instance { get; private set; }
        public IHttpClient HttpClient { get; }

        [ImportingConstructor]
        public Globals(IHttpClient httpClient)
        {
            Globals.Instance = this;
            this.HttpClient = httpClient;
        }

        public void Dispose()
        {
            Globals.Instance = null;
        }

        void IAppListener.OnStartup(IApp app)
        {
        }

        void IAppListener.OnOpened(IApp app, IWindow window)
        {
        }

        void IAppListener.OnClosing(IApp app, IWindow window)
        {
        }

        void IAppListener.OnExit(IApp app)
        {
        }
    }
}
