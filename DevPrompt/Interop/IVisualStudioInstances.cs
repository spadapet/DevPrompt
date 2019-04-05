using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl

    [Guid("8a4b9d86-da6e-4d15-9ee0-58080da25282")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IVisualStudioInstances
    {
        int GetCount();
        [return: MarshalAs(UnmanagedType.Interface)]
        IVisualStudioInstance GetValue(int index);
    }
}
