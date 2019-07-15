using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace DevPrompt.Api
{
    /// <summary>
    /// The global app
    /// </summary>
    public interface IApp
    {
        IAppSettings Settings { get; }
        IEnumerable<IWindow> Windows { get; }
        Dispatcher Dispatcher { get; }

        bool IsElevated { get; }
        bool IsMainProcess { get; }
        bool IsMicrosoftDomain { get; }

        IEnumerable<GrabProcess> GrabProcesses { get; }
        void GrabProcess(int id);
        IProcessHost CreateProcessHost(IntPtr parentHwnd);

        /// <summary>
        /// Return value is also a dynamic for runtime type checking
        /// </summary>
        IJsonValue ParseJson(string json);
    }
}
