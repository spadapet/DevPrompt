using System.Diagnostics;

namespace DevPrompt.Utility
{
    internal class GrabConsoleInfo
    {
        public Process Process { get; }

        public GrabConsoleInfo(Process process)
        {
            this.Process = process;
        }
    }
}
