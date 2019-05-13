using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl

    /// <summary>
    /// Allows managed code to talk to the native process host object
    /// </summary>
    [Guid("cedddf4b-b229-4a17-8b10-140e53464efd")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IProcessHost
    {
        void Dispose();
        void Activate();
        void Deactivate();
        void Show();
        void Hide();
        void Focus();
        IntPtr GetWindow();

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcess RunProcess(
            [MarshalAs(UnmanagedType.LPWStr)] string executable,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            [MarshalAs(UnmanagedType.LPWStr)] string startingDirectory);

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcess RestoreProcess([MarshalAs(UnmanagedType.LPWStr)] string state);

        [return: MarshalAs(UnmanagedType.Interface)]
        IProcess CloneProcess([MarshalAs(UnmanagedType.Interface)] IProcess process);

        [return: MarshalAs(UnmanagedType.VariantBool)]
        bool ContainsProcess([MarshalAs(UnmanagedType.Interface)] IProcess process);
    }
}
