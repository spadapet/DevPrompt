using DevPrompt.Plugins;
using System.Composition;

namespace DevOps
{
    [Export(typeof(IAppListener))]
    public class AppListener : IAppListener
    {
        private Globals globals;

        public AppListener()
        {
        }

        void IAppListener.OnStartup(IApp app)
        {
            this.globals = new Globals(app);
        }

        void IAppListener.OnExit(IApp app)
        {
            if (this.globals != null)
            {
                this.globals.Dispose();
                this.globals = null;
            }
        }
    }
}
