﻿using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    // MUST match DevPrompt.idl

    /// <summary>
    /// Managed interface for native Visual Studio instances
    /// </summary>
    [Guid("4c1047a1-e701-4024-aeed-08a38c70584e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVisualStudioInstance
    {
        [return: MarshalAs(UnmanagedType.BStr)] string GetInstallationName();
        [return: MarshalAs(UnmanagedType.BStr)] string GetInstanceId();
        [return: MarshalAs(UnmanagedType.BStr)] string GetInstallationPath();
        [return: MarshalAs(UnmanagedType.BStr)] string GetProductPath();
        [return: MarshalAs(UnmanagedType.BStr)] string GetInstallationVersion();
        [return: MarshalAs(UnmanagedType.BStr)] string GetChannelId();
    }
}
