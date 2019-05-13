using System;

namespace DevPrompt.Interop
{
    /// <summary>
    /// Wrapper for native process hosts
    /// </summary>
    internal class NativeProcessHost : Api.PropertyNotifier, Api.IProcessHost
    {
        public IProcessHost Host { get; }
        private IProcessCache processCache;

        public NativeProcessHost(IProcessCache processCache, IProcessHost host)
        {
            this.Host = host;
            this.processCache = processCache;
        }

        public void Dispose()
        {
            this.Host.Dispose();
        }

        public void Activate()
        {
            this.Host.Activate();
        }

        public void Deactivate()
        {
            this.Host.Deactivate();
        }

        public void Show()
        {
            this.Host.Show();
        }

        public void Hide()
        {
            this.Host.Hide();
        }

        public IntPtr Hwnd
        {
            get
            {
                return this.Host.GetWindow();
            }
        }

        public void Focus()
        {
            this.Host.Focus();
        }

        public NativeProcess RunProcess(string executable, string arguments, string startingDirectory)
        {
            if (this.Host.RunProcess(executable, arguments, startingDirectory) is IProcess process)
            {
                return this.processCache.GetNativeProcess(process);
            }

            return null;
        }

        public NativeProcess RestoreProcess(string state)
        {
            if (this.Host.RestoreProcess(state) is IProcess process)
            {
                return this.processCache.GetNativeProcess(process);
            }

            return null;
        }

        public NativeProcess CloneProcess(NativeProcess process)
        {
            if (this.Host.CloneProcess(process.Process) is IProcess clone)
            {
                return this.processCache.GetNativeProcess(clone);
            }

            return null;
        }

        public bool ContainsProcess(NativeProcess process)
        {
            return this.Host.ContainsProcess(process.Process);
        }

        public override int GetHashCode()
        {
            return this.Hwnd.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is NativeProcessHost other && this.Hwnd == other.Hwnd;
        }

        Api.IProcess Api.IProcessHost.RunProcess(string executable, string arguments, string startingDirectory)
        {
            return this.RunProcess(executable, arguments, startingDirectory);
        }

        Api.IProcess Api.IProcessHost.RestoreProcess(string state)
        {
            return this.RestoreProcess(state);
        }

        Api.IProcess Api.IProcessHost.CloneProcess(Api.IProcess process)
        {
            if (process is NativeProcess nativeProcess)
            {
                return this.CloneProcess(nativeProcess);
            }

            return null;
        }

        bool Api.IProcessHost.ContainsProcess(Api.IProcess process)
        {
            return process is NativeProcess nativeProcess && this.ContainsProcess(nativeProcess);
        }
    }
}
