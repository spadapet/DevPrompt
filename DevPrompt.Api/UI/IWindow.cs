using System;
using System.Collections.Generic;

namespace DevPrompt.Api
{
    /// <summary>
    /// Public access to the main window
    /// </summary>
    public interface IWindow
    {
        IApp App { get; }
        IntPtr Handle { get; }
        IInfoBar InfoBar { get; }
        IProgressBar ProgressBar { get; }

        void Focus();
        void ShowSettingsDialog(SettingsTabType tab);

        // Workspaces
        IEnumerable<IWorkspaceHolder> Workspaces { get; }
        IWorkspaceHolder ActiveWorkspace { get; set; }
        IWorkspaceHolder FindWorkspace(Guid id);
        IWorkspaceHolder AddWorkspace(IWorkspace workspace, bool activate);
        IWorkspaceHolder AddWorkspace(IWorkspaceProvider provider, bool activate);
        IWorkspaceHolder AddWorkspace(IWorkspaceProvider provider, IWorkspaceSnapshot snapshot, bool activate);
        void RemoveWorkspace(IWorkspaceHolder workspace);
    }
}
