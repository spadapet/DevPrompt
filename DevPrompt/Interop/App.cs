using System;
using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    internal static class App
    {
        [DllImport("DevNative64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateApp")]
        private static extern void CreateApp64(IAppHost host, out IApp app);

        [DllImport("DevNative32", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateApp")]
        private static extern void CreateApp32(IAppHost host, out IApp app);

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

        public static IApp CreateApp(IAppHost host, out string errorMessage)
        {
            return App.CallDevNative(
                () =>
                {
                    App.CreateApp64(host, out IApp app64);
                    return app64;
                },
                () =>
                {
                    App.CreateApp32(host, out IApp app32);
                    return app32;
                },
                out IApp app, out errorMessage) ? app : null;
        }

        public static IVisualStudioInstances CreateVisualStudioInstances()
        {
            return App.CallDevNative(
                () =>
                {
                    App.CreateVisualStudioInstances64(out IVisualStudioInstances instances64);
                    return instances64;
                },
                () =>
                {
                    App.CreateVisualStudioInstances32(out IVisualStudioInstances instances32);
                    return instances32;
                },
                out IVisualStudioInstances instances, out string errorMessage) ? instances : null;
        }
    }
}
