using System.Collections.Generic;
using System.Composition;
using System.Linq;

namespace DevPrompt.Interop
{
    /// <summary>
    /// Keeps the cached NativeProcess up to date with changes to the native IProcess
    /// </summary>
    [Export(typeof(IProcessListener))]
    internal sealed class NativeProcessListener : IProcessListener
    {
        private readonly IProcessCache processCache;
        private readonly Api.IProcessListener[] processListeners;

        [ImportingConstructor]
        public NativeProcessListener(
            IProcessCache processCache,
            [ImportMany] IEnumerable<Api.IProcessListener> processListeners)
        {
            this.processCache = processCache;
            this.processListeners = processListeners?.ToArray() ?? new Api.IProcessListener[0];
        }

        public void OnProcessOpening(IProcess process, bool activate, string path)
        {
            NativeProcess wrapper = this.processCache.GetNativeProcess(process);
            foreach (Api.IProcessListener listener in this.processListeners)
            {
                listener.OnProcessOpening(wrapper, activate, path);
            }
        }

        public void OnProcessClosing(IProcess process)
        {
            NativeProcess wrapper = this.processCache.GetNativeProcess(process);
            foreach (Api.IProcessListener listener in this.processListeners)
            {
                listener.OnProcessClosing(wrapper);
            }

            this.processCache.RemoveNativeProcess(process);
        }

        public void OnProcessEnvChanged(IProcess process, string env)
        {
            this.processCache.GetNativeProcess(process).Environment = env;
        }

        public void OnProcessTitleChanged(IProcess process, string title)
        {
            this.processCache.GetNativeProcess(process).Title = title;
        }
    }
}
