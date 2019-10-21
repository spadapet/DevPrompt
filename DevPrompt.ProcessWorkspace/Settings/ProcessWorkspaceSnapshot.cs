using System.Runtime.Serialization;

namespace DevPrompt.ProcessWorkspace.Settings
{
    /// <summary>
    /// Saves the state of a process workspace during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    internal class ProcessWorkspaceSnapshot : TabWorkspaceSnapshot, Api.IWorkspaceSnapshot
    {
        public ProcessWorkspaceSnapshot(ProcessWorkspace workspace)
            : base(workspace)
        {
        }

        public ProcessWorkspaceSnapshot(ProcessWorkspaceSnapshot copyFrom)
            : base(copyFrom)
        {
        }

        public override Api.IWorkspaceSnapshot Clone()
        {
            return new ProcessWorkspaceSnapshot(this);
        }

        public override Api.IWorkspace Restore(Api.IWindow window)
        {
            return new ProcessWorkspace(window, this);
        }
    }
}
