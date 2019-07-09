using System.Diagnostics;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// A plugin that's downloaded from NuGet
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Id}")]
    internal class NuGetPluginSettings : Api.PropertyNotifier
    {
        private string id;
        private string version;
        private bool enabled;

        public NuGetPluginSettings()
        {
            this.id = string.Empty;
            this.version = string.Empty;
            this.enabled = true;
        }

        public NuGetPluginSettings(NuGetPluginSettings copyFrom)
        {
            this.id = copyFrom.id;
            this.version = copyFrom.version;
            this.enabled = copyFrom.enabled;
        }

        public NuGetPluginSettings Clone()
        {
            return new NuGetPluginSettings(this);
        }

        [DataMember]
        public string Id
        {
            get => this.id;
            set => this.SetPropertyValue(ref this.id, value ?? string.Empty);
        }

        [DataMember]
        public string Version
        {
            get => this.version;
            set => this.SetPropertyValue(ref this.version, value);
        }

        [DataMember]
        public bool Enabled
        {
            get => this.enabled;
            set => this.SetPropertyValue(ref this.enabled, value);
        }
    }
}
