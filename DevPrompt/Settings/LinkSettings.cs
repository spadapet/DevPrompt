using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// A single "Link" customizable by the user
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Name}")]
    internal class LinkSettings : PropertyNotifier, IEquatable<LinkSettings>
    {
        private string name;
        private string address;

        public LinkSettings()
        {
            this.name = DevPrompt.Utility.CommandUtility.SeparatorName;
            this.address = string.Empty;
        }

        public LinkSettings(LinkSettings copyFrom)
        {
            this.name = copyFrom.Name;
            this.address = copyFrom.Address;
        }

        public LinkSettings Clone()
        {
            return new LinkSettings(this);
        }

        public bool Equals(LinkSettings other)
        {
            return this.name == other.name && this.address == other.address;
        }

        [DataMember]
        public string Name
        {
            get => this.name;
            set => this.SetPropertyValue(ref this.name, value ?? string.Empty);
        }

        [DataMember]
        public string Address
        {
            get => this.address;
            set => this.SetPropertyValue(ref this.address, value ?? string.Empty);
        }
    }
}
