using DevPrompt.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DevPrompt.UI
{
    /// <summary>
    /// Hooks the native process host window into WPF
    /// </summary>
    public class ProcessHostWindow : HwndHost
    {
        internal IProcessHost ProcessHost { get; private set; }

        public ProcessHostWindow()
        {
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

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            this.ProcessHost?.DpiChanged(oldDpi.DpiScaleX, newDpi.DpiScaleX);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            this.ProcessHost = App.Current.NativeApp?.CreateProcessHostWindow(hwndParent.Handle);

            if (this.IsFocused)
            {
                this.ProcessHost?.Focus();
            }

            return new HandleRef(null, this.ProcessHost?.GetWindow() ?? IntPtr.Zero);
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
