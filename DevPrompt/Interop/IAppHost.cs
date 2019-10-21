using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl

    /// <summary>
    /// Allows native code to talk to the managed app
    /// </summary>
    [Guid("42d16e5c-8acf-4dcb-882d-b41974190e53")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAppHost
    {
        int CanGrab([MarshalAs(UnmanagedType.LPWStr)] string exePath, [MarshalAs(UnmanagedType.VariantBool)] bool automatic);
        void TrackEvent([MarshalAs(UnmanagedType.LPWStr)] string eventName);

        // Processes
        void OnProcessOpening(IProcess process, [MarshalAs(UnmanagedType.VariantBool)] bool activate, [MarshalAs(UnmanagedType.LPWStr)] string path);
        void OnProcessClosing(IProcess process);
        void OnProcessEnvChanged(IProcess process, [MarshalAs(UnmanagedType.LPWStr)] string env);
        void OnProcessTitleChanged(IProcess process, [MarshalAs(UnmanagedType.LPWStr)] string title);
    }
}
