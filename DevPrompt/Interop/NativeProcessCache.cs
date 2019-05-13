using System.Collections.Generic;
using System.Composition;

namespace DevPrompt.Interop
{
    [Export(typeof(IProcessCache))]
    internal class NativeProcessCache : IProcessCache, IEqualityComparer<IProcess>
    {
        private Dictionary<IProcess, NativeProcess> processes;

        public NativeProcessCache()
        {
            this.processes = new Dictionary<IProcess, NativeProcess>(this);
        }

        public NativeProcess GetNativeProcess(IProcess process)
        {
            NativeProcess wrapper = null;
            if (process != null && !this.processes.TryGetValue(process, out wrapper))
            {
                wrapper = new NativeProcess(process);
                this.processes[process] = wrapper;
            }

            return wrapper;
        }

        public bool RemoveNativeProcess(IProcess process)
        {
            return process != null && this.processes.Remove(process);
        }

        bool IEqualityComparer<IProcess>.Equals(IProcess x, IProcess y)
        {
            if (x is IProcess process1)
            {
                return y is IProcess process2 && process1.GetWindow() == process2.GetWindow();
            }

            return x == null && y == null;
        }

        int IEqualityComparer<IProcess>.GetHashCode(IProcess obj)
        {
            return obj.GetWindow().GetHashCode();
        }
    }
}
