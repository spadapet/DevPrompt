using DevPrompt.ProcessWorkspace.UI.ViewModels;
using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.UI.ViewModels;
using System;
using System.Runtime.Serialization;

namespace DevPrompt.ProcessWorkspace.Settings
{
    /// <summary>
    /// Saves the state of a process tab during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    internal class ProcessSnapshot : PropertyNotifier, Api.ITabSnapshot
    {
        Guid Api.ITabSnapshot.Id => Guid.Empty;
        string Api.ITabSnapshot.Name => this.CachedName;
        string Api.ITabSnapshot.Tooltip => this.CachedTooltip;

        private string cachedName;
        private string cachedTooltip;
        private string rawName;
        private string state;

        public ProcessSnapshot()
        {
            this.Initialize();
        }

        internal ProcessSnapshot(ProcessTab process)
            : this()
        {
            this.cachedName = process.Name;
            this.cachedTooltip = process.Tooltip;
            this.rawName = process.RawName;
            this.state = process.State;
        }

        public ProcessSnapshot(ProcessSnapshot copyFrom)
            : this()
        {
            this.cachedName = copyFrom.cachedName;
            this.cachedTooltip = copyFrom.cachedTooltip;
            this.rawName = copyFrom.rawName;
            this.state = copyFrom.state;
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.cachedName = string.Empty;
            this.cachedTooltip = string.Empty;
            this.rawName = string.Empty;
            this.state = string.Empty;
        }

        Api.ITabSnapshot Api.ITabSnapshot.Clone()
        {
            return new ProcessSnapshot(this);
        }

        Api.ITab Api.ITabSnapshot.Restore(Api.IWindow window, Api.ITabWorkspace workspace)
        {
            if (!string.IsNullOrEmpty(this.State) &&
                workspace is Api.IProcessWorkspace processWorkspace &&
                processWorkspace.RestoreProcess(this.State, this.RawName) is ITabVM tab)
            {
                return tab.Tab;
            }

            return null;
        }

        [DataMember]
        public string RawName
        {
            get
            {
                return this.rawName;
            }

            set
            {
                this.SetPropertyValue(ref this.rawName, value ?? string.Empty);
            }
        }

        [DataMember]
        public string CachedName
        {
            get
            {
                return this.cachedName;
            }

            set
            {
                this.SetPropertyValue(ref this.cachedName, value ?? string.Empty);
            }
        }

        [DataMember]
        public string CachedTooltip
        {
            get
            {
                return this.cachedTooltip;
            }

            set
            {
                this.SetPropertyValue(ref this.cachedTooltip, value ?? string.Empty);
            }
        }

        [DataMember]
        public string State
        {
            get
            {
                return this.state;
            }

            set
            {
                this.SetPropertyValue(ref this.state, value ?? string.Empty);
            }
        }
    }
}
