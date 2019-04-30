using DevPrompt.Plugins;
using System.Composition;

namespace DevOps
{
    [Export(typeof(IAppListener))]
    public class AppListener : IAppListener
    {
        public AppListener()
        {
        }

        void IAppListener.OnExit(IApp app)
        {
        }

        void IAppListener.OnStartup(IApp app)
        {
        }
    }
}
