using System.Runtime.InteropServices;

namespace DevPrompt.Interop
{
    internal class NativeMethods
    {
        [DllImport("user32")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);
    }
}
