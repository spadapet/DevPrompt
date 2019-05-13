using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DevPrompt.UI.Controls
{
    /// <summary>
    /// Hooks the native process host window into WPF
    /// </summary>
    internal class ProcessHostWindow : HwndHost
    {
        public Api.IProcessHost ProcessHost { get; private set; }
        private Api.IApp app;

        public ProcessHostWindow(Api.IApp app)
        {
            this.app = app;
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
            this.ProcessHost = this.app.CreateProcessHost(hwndParent.Handle);

            if (this.IsFocused)
            {
                this.ProcessHost?.Focus();
            }

            return new HandleRef(null, this.ProcessHost?.Hwnd ?? IntPtr.Zero);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            this.ProcessHost?.Dispose();
            this.ProcessHost = null;
        }

        protected override void OnGotFocus(RoutedEventArgs args)
        {
            this.ProcessHost?.Focus();
            base.OnGotFocus(args);
        }
    }
}
