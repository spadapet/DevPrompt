using DevPrompt.UI;
using DevPrompt.Utility;
using System;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves the state of a console during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    public class ConsoleSnapshot : PropertyNotifier, ICloneable
    {
        private string tabName;
        private string state;

        public ConsoleSnapshot()
            : this((ProcessVM)null)
        {
        }

        internal ConsoleSnapshot(ProcessVM process)
        {
            this.tabName = process?.TabName ?? string.Empty;
            this.state = process?.Process?.GetState() ?? string.Empty;
        }

        public ConsoleSnapshot(ConsoleSnapshot copyFrom)
        {
            this.tabName = copyFrom.tabName;
            this.state = copyFrom.state;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public ConsoleSnapshot Clone()
        {
            return new ConsoleSnapshot(this);
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
