using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl

    /// <summary>
    /// Allows managed code to talk to the native app
    /// </summary>
    [Guid("151c2791-131f-43b6-9384-0f5a8c1c9461")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IApp
    {
        void Dispose();
        void Activate();
        void Deactivate();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetGrabProcesses();
        void GrabProcess(int id);

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcessHost CreateProcessHostWindow(IntPtr parentHwnd);
    }

    internal static class App
    {
        [DllImport("DevNative", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CreateApp(IAppHost host, out IApp app);
    }
}
