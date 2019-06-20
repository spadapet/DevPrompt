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
            NativeMethods.SafeComCall(this.App.Dispose);
            Marshal.FinalReleaseComObject(this.App);
        }

        public void Activate()
        {
            NativeMethods.SafeComCall(this.App.Activate);
        }

        public void Deactivate()
        {
            NativeMethods.SafeComCall(this.App.Deactivate);
        }

        public string GrabProcesses
        {
            get
            {
                return NativeMethods.SafeComCall(this.App.GetGrabProcesses, string.Empty);
            }
        }

        public void GrabProcess(int id)
        {
            NativeMethods.SafeComCall(() => this.App.GrabProcess(id));
        }

        public NativeProcessHost CreateProcessHost(IProcessCache processCache, IntPtr parentHwnd)
        {
            if (NativeMethods.SafeComCall(() => this.App.CreateProcessHostWindow(parentHwnd)) is IProcessHost host)
            {
                return new NativeProcessHost(processCache, host);
            }

            return null;
        }

        public void MainWindowProc(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp)
        {
            NativeMethods.SafeComCall(() => this.App.MainWindowProc(hwnd, msg, wp, lp));
        }
    }
}
