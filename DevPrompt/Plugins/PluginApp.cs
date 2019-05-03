using System.Collections.Generic;
using System.Composition;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Plugins can [Import] this to access the app
    /// </summary>
    [Export(typeof(IApp))]
    internal class PluginApp : IApp
    {
        public PluginApp()
        {
        }

        T IApp.GetExport<T>()
        {
            return App.Current.GetExport<T>();
        }

        IEnumerable<T> IApp.GetExports<T>()
        {
            return App.Current.GetExports<T>();
        }
    }
}
