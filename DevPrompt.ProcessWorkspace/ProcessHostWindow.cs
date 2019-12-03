using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DevPrompt.ProcessWorkspace
{
    /// <summary>
    /// Hooks the native process host window into WPF
    /// </summary>
    internal sealed class ProcessHostWindow : HwndHost
    {
        public Api.IProcessHost ProcessHost { get; private set; }
        private readonly Api.IApp app;

        public ProcessHostWindow(Api.IApp app)
        {
            this.app = app;
            this.Focusable = false;
        }

        public void OnActivated()
        {
            this.ProcessHost?.Activate();
        }

        public void OnDeactivated()
        {
            this.ProcessHost?.Deactivate();
        }

        public void Show()
        {
            this.ProcessHost?.Show();
        }

        public void Hide()
        {
            this.ProcessHost?.Hide();
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this.ProcessHost = this.app.AppProcesses.CreateProcessHost(hwndParent.Handle);
            return new HandleRef(null, this.ProcessHost?.Hwnd ?? IntPtr.Zero);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            this.ProcessHost?.Dispose();
            this.ProcessHost = null;
        }
    }
}
