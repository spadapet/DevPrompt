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
        public App App { get; set; }

        public PluginApp()
        {
        }

        T IApp.GetExport<T>()
        {
            return this.App.GetExport<T>();
        }

        IEnumerable<T> IApp.GetExports<T>()
        {
            return this.App.GetExports<T>();
        }
    }
}
