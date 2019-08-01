using System;
using System.Collections.Generic;
using System.Windows;

namespace DevPrompt.Api
{
    /// <summary>
    /// Public access to the main window
    /// </summary>
    public interface IWindow
    {
        IApp App { get; }
        Window Window { get; }
        void RunExternalProcess(string path, string arguments = null);

        // Workspaces
        IEnumerable<IWorkspaceVM> Workspaces { get; }
        IWorkspaceVM ActiveWorkspace { get; set; }
        IWorkspaceVM FindWorkspace(Guid id);
        void AddWorkspace(IWorkspaceVM workspace, bool activate);
        void RemoveWorkspace(IWorkspaceVM workspace);

        // State
        void SetError(Exception exception, string text = null);
        IDisposable BeginLoading(Action cancelAction = null, string text = null);
        void CancelLoading();
        bool Loading { get; }
    }
}
