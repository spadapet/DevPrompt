using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl

    /// <summary>
    /// Allows managed code to talk to the native process object
    /// </summary>
    [Guid("e3b1d8b5-bce5-4522-ad92-44ce6edda69c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IProcess
    {
        void Dispose();
        void Detach();
        void Activate();
        void Deactivate();
        IntPtr GetWindow();

        [return: MarshalAs(UnmanagedType.BStr)] string GetWindowTitle();
        [return: MarshalAs(UnmanagedType.BStr)] string GetExe();
        [return: MarshalAs(UnmanagedType.BStr)] string GetEnv();
        [return: MarshalAs(UnmanagedType.BStr)] string GetAliases();
        [return: MarshalAs(UnmanagedType.BStr)] string GetCurrentDirectory();
        [return: MarshalAs(UnmanagedType.BStr)] string GetColorTable();
        void SetColorTable([MarshalAs(UnmanagedType.LPWStr)] string value);

        void Focus();
        void SystemCommandDefaults();
        void SystemCommandProperties();
    }
}
