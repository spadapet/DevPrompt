using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    /// <summary>
    /// Wrapper for native app
    /// </summary>
    internal class NativeApp : IDisposable
    {
        internal IApp App { get; }

        internal NativeApp(IApp app)
        {
            this.App = app;
        }

        public void Dispose()
        {
            this.App.Dispose();
            Marshal.FinalReleaseComObject(this.App);
        }

        public void Activate()
        {
            this.App.Activate();
        }

        public void Deactivate()
        {
            this.App.Deactivate();
        }

        public string GrabProcesses
        {
            get
            {
                return this.App.GetGrabProcesses();
            }
        }

        public void GrabProcess(int id)
        {
            this.App.GrabProcess(id);
        }

        public NativeProcessHost CreateProcessHost(IProcessCache processCache, IntPtr parentHwnd)
        {
            if (this.App.CreateProcessHostWindow(parentHwnd) is IProcessHost host)
            {
                return new NativeProcessHost(processCache, host);
            }

            return null;
        }

        public void MainWindowProc(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp)
        {
            this.App.MainWindowProc(hwnd, msg, wp, lp);
        }
    }
}
