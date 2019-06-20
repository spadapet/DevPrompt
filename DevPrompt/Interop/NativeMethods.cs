using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    /// <summary>
    /// Calls into global exported methods from native DevNative DLL
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("DevNative64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateApp")]
        private static extern void CreateApp64(IAppHost host, [MarshalAs(UnmanagedType.Bool)] bool elevated, out IApp app);

        [DllImport("DevNative32", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateApp")]
        private static extern void CreateApp32(IAppHost host, [MarshalAs(UnmanagedType.Bool)] bool elevated, out IApp app);

        [DllImport("DevNative64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateVisualStudioInstances")]
        private static extern void CreateVisualStudioInstances64(out IVisualStudioInstances obj);

        [DllImport("DevNative32", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateVisualStudioInstances")]
        private static extern void CreateVisualStudioInstances32(out IVisualStudioInstances obj);

        private static bool CallDevNative<T>(Func<T> func64, Func<T> func32, out T result, out string errorMessage) where T : class
        {
            try
            {
                result = Environment.Is64BitProcess ? func64() : func32();
                errorMessage = null;
            }
            catch (TypeLoadException ex)
            {
                // Probably missing a DLL
                result = null;
                errorMessage = ex.Message;
            }

            return result != null;
        }

        public static NativeApp CreateApp(IAppHost host, out string errorMessage)
        {
            return NativeMethods.CallDevNative(
                () =>
                {
                    NativeMethods.CreateApp64(host, Program.IsElevated, out IApp app64);
                    return app64;
                },
                () =>
                {
                    NativeMethods.CreateApp32(host, Program.IsElevated, out IApp app32);
                    return app32;
                },
                out IApp app, out errorMessage) ? new NativeApp(app) : null;
        }

        public static IVisualStudioInstances CreateVisualStudioInstances()
        {
            return NativeMethods.CallDevNative(
                () =>
                {
                    NativeMethods.CreateVisualStudioInstances64(out IVisualStudioInstances instances64);
                    return instances64;
                },
                () =>
                {
                    NativeMethods.CreateVisualStudioInstances32(out IVisualStudioInstances instances32);
                    return instances32;
                },
                out IVisualStudioInstances instances, out string errorMessage) ? instances : null;
        }

        public static void SafeComCall(Action action)
        {
            try
            {
                action();
            }
            catch (COMException)
            {
                Debug.Fail("COM call failed");
            }
        }

        public static T SafeComCall<T>(Func<T> func, T returnOnFailure = default)
        {
            try
            {
                return func();
            }
            catch (COMException)
            {
                Debug.Fail("COM call failed");
                return returnOnFailure;
            }
        }
    }
}
