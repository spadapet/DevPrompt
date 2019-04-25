using DevPrompt.Interop;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves the state of a console during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    public class ConsoleSnapshot : PropertyNotifier, ICloneable, ITabSnapshot
    {
        private string tabName;
        private string state;

        public ConsoleSnapshot()
            : this((ProcessVM)null)
        {
        }

        internal ConsoleSnapshot(ProcessVM process)
        {
            this.Initialize();

            this.tabName = process?.TabName ?? string.Empty;
            this.state = process?.Process?.GetState() ?? string.Empty;
        }

        public ConsoleSnapshot(ConsoleSnapshot copyFrom)
        {
            this.Initialize();

            this.tabName = copyFrom.tabName;
            this.state = copyFrom.state;
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.tabName = string.Empty;
            this.state = string.Empty;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        ITabSnapshot ITabSnapshot.Clone()
        {
            return this.Clone();
        }

        public ConsoleSnapshot Clone()
        {
            return new ConsoleSnapshot(this);
        }

        ITabVM ITabSnapshot.Restore(IMainWindowVM window)
        {
            if (string.IsNullOrEmpty(this.State))
            {
                return null;
            }

            IProcess process = window.ProcessHost?.RestoreProcess(this.State);
            ITabVM tab = window.FindTab(process);

            if (tab is ProcessVM processTab)
            {
                processTab.TabName = this.TabName;
            }

            return tab;
        }

        [DataMember]
        public string TabName
        {
            get
            {
                return this.tabName;
            }

            set
            {
                this.SetPropertyValue(ref this.tabName, value ?? string.Empty);
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
