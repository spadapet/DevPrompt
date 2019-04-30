using System.Collections.Generic;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Allows plugins to call into the app
    /// </summary>
    public interface IApp
    {
        T GetExport<T>();
        IEnumerable<T> GetExports<T>();
    }
}
