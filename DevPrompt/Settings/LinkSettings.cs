using DevPrompt.Utility;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    [DataContract]
    [DebuggerDisplay("{Name}")]
    public class LinkSettings : PropertyNotifier, ICloneable
    {
        private string name;
        private string address;

        public LinkSettings()
        {
            this.name = DevPrompt.Utility.Helpers.SeparatorName;
            this.address = string.Empty;
        }

        public LinkSettings(LinkSettings copyFrom)
        {
            this.name = copyFrom.Name;
            this.address = copyFrom.Address;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public LinkSettings Clone()
        {
            return new LinkSettings(this);
        }

        [DataMember]
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.SetPropertyValue(ref this.name, value ?? string.Empty);
            }
        }

        [DataMember]
        public string Address
        {
            get
            {
                return this.address;
            }

            set
            {
                this.SetPropertyValue(ref this.address, value ?? string.Empty);
            }
        }
    }
}
