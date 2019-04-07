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
    interface IAppHost
    {
        IntPtr GetMainWindow();
        int CanGrab([MarshalAs(UnmanagedType.LPWStr)] string exePath, [MarshalAs(UnmanagedType.VariantBool)] bool automatic);
        void OnSystemShutdown();

        // Keyboard handling (since WPF will not see most key presses since focus will mostly be in a hosted process)
        void OnAltLetter(int vk);
        void OnAlt();

        // Processes
        void OnProcessOpening(IProcess process, [MarshalAs(UnmanagedType.VariantBool)] bool activate, [MarshalAs(UnmanagedType.LPWStr)] string path);
        void OnProcessClosing(IProcess process);
        void OnProcessEnvChanged(IProcess process, [MarshalAs(UnmanagedType.LPWStr)] string env);
        void OnProcessTitleChanged(IProcess process, [MarshalAs(UnmanagedType.LPWStr)] string env);
        void CloneActiveProcess();
        void CloseActiveProcess();
        void DetachActiveProcess();
        void SetTabName();

        // Ctrl-Tab and Ctrl-Shift-Tab
        void TabCycleStop();
        void TabCycleNext();
        void TabCyclePrev();
    }
}
