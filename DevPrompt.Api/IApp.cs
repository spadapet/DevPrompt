using System;
using System.Collections.Generic;

namespace DevPrompt.Api
{
    /// <summary>
    /// The global app
    /// </summary>
    public interface IApp
    {
        IAppSettings Settings { get; }
        IAppUpdate AppUpdate { get; }
        ITelemetry Telemetry { get; }
        IVisualStudioSetup VisualStudioSetup { get; }
        IWindow ActiveWindow { get; }
        IEnumerable<IWindow> Windows { get; }

        bool IsElevated { get; }
        bool IsMainProcess { get; }
        bool IsMicrosoftDomain { get; }

        IEnumerable<GrabProcess> GrabProcesses { get; }
        void GrabProcess(int id);
        IProcessHost CreateProcessHost(IntPtr parentHwnd);
        void RunExternalProcess(string path, string arguments = null);
    }
}
