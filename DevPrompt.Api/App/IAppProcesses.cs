using System;
using System.Collections.Generic;

namespace DevPrompt.Api
{
    /// <summary>
    /// Process related helper methods
    /// </summary>
    public interface IAppProcesses
    {
        IEnumerable<GrabProcess> GrabProcesses { get; }
        void GrabProcess(int id);
        IProcessHost CreateProcessHost(IntPtr parentHwnd);
        void RunExternalProcess(string path, string arguments = null);
    }
}
