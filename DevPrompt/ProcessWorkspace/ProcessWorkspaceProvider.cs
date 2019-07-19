using System;
using System.Composition;

namespace DevPrompt.ProcessWorkspace
{
    [Export(typeof(Api.IWorkspaceProvider))]
    [Api.Order(Api.Constants.HigherPriority)]
    internal class ProcessWorkspaceProvider : Api.IWorkspaceProvider
    {
        public Guid WorkspaceId => Api.Constants.ProcessWorkspaceId;
        public string WorkspaceName => ProcessWorkspace.StaticName;
        public string WorkspaceTooltip => ProcessWorkspace.StaticTooltip;

        public ProcessWorkspaceProvider()
        {
        }

        public Api.IWorkspace CreateWorkspace(Api.IWindow window)
        {
            return new ProcessWorkspace(window);
        }
    }
}
