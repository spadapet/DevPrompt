using DevPrompt.Settings;
using System.Runtime.Serialization;

namespace DevPrompt.ProcessWorkspace
{
    /// <summary>
    /// Saves the state of a process workspace during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    internal class ProcessWorkspaceSnapshot : TabWorkspaceSnapshot, Api.IWorkspaceSnapshot
    {
        internal ProcessWorkspaceSnapshot(ProcessWorkspace workspace)
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
