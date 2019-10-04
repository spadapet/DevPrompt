using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    [DataContract]
    internal class WorkspaceSnapshot : PropertyNotifier
    {
        private Guid id;
        private Api.IWorkspaceSnapshot snapshot;

        public WorkspaceSnapshot()
        {
            this.Initialize();
        }

        public WorkspaceSnapshot(Guid id, Api.IWorkspaceSnapshot snapshot)
            : this()
        {
            this.id = id;
            this.snapshot = snapshot;
        }

        public WorkspaceSnapshot(WorkspaceSnapshot copyFrom)
            : this()
        {
            this.CopyFrom(copyFrom);
        }

        public WorkspaceSnapshot Clone()
        {
            return new WorkspaceSnapshot(this);
        }

        public void CopyFrom(WorkspaceSnapshot copyFrom)
        {
            this.id = copyFrom.id;
            this.snapshot = copyFrom.snapshot;
        }

        [DataMember]
        public Guid Id
        {
            get => this.id;
            set => this.SetPropertyValue(ref this.id, value);
        }

        [DataMember]
        public Api.IWorkspaceSnapshot Snapshot
        {
            get => this.snapshot;
            set => this.SetPropertyValue(ref this.snapshot, value);
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
        }
    }
}
