using System.Collections.Generic;

namespace DevPrompt.Api
{
    /// <summary>
    /// The global app, this can imported by extensions
    /// </summary>
    public interface IApp
    {
        IAppSettings Settings { get; }
        IAppUpdate AppUpdate { get; }
        IAppProcesses AppProcesses { get; }
        ITelemetry Telemetry { get; }
        IVisualStudioSetup VisualStudioSetup { get; }
        IWindow ActiveWindow { get; }
        IEnumerable<IWindow> Windows { get; }

        bool IsElevated { get; }
        bool IsMainProcess { get; }
        bool IsMicrosoftDomain { get; }
    }
}
