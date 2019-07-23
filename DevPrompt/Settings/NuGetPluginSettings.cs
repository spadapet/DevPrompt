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
        private string title;
        private string description;
        private string summary;
        private string projectUrl;
        private string iconUrl;
        private string authors;
        private string version;
        private string versionRegistrationUrl;
        private string path;
        private bool enabled;

        public NuGetPluginSettings()
        {
            this.id = string.Empty;
            this.title = string.Empty;
            this.description = string.Empty;
            this.summary = string.Empty;
            this.projectUrl = string.Empty;
            this.iconUrl = string.Empty;
            this.authors = string.Empty;
            this.version = string.Empty;
            this.versionRegistrationUrl = string.Empty;
            this.path = string.Empty;
            this.enabled = true;
        }

        public NuGetPluginSettings(NuGetPluginSettings copyFrom)
        {
            this.id = copyFrom.id;
            this.title = copyFrom.title;
            this.description = copyFrom.description;
            this.summary = copyFrom.summary;
            this.projectUrl = copyFrom.projectUrl;
            this.iconUrl = copyFrom.iconUrl;
            this.authors = copyFrom.authors;
            this.version = copyFrom.version;
            this.versionRegistrationUrl = copyFrom.versionRegistrationUrl;
            this.path = copyFrom.path;
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
        public string Path
        {
            get => this.path;
            set => this.SetPropertyValue(ref this.path, value ?? string.Empty);
        }

        [DataMember]
        public bool Enabled
        {
            get => this.enabled;
            set => this.SetPropertyValue(ref this.enabled, value);
        }

        [DataMember]
        public string Title
        {
            get => this.title;
            set => this.SetPropertyValue(ref this.title, value ?? string.Empty);
        }

        [DataMember]
        public string Description
        {
            get => this.description;
            set => this.SetPropertyValue(ref this.description, value ?? string.Empty);
        }

        [DataMember]
        public string Summary
        {
            get => this.summary;
            set => this.SetPropertyValue(ref this.summary, value ?? string.Empty);
        }

        [DataMember]
        public string ProjectUrl
        {
            get => this.projectUrl;
            set => this.SetPropertyValue(ref this.projectUrl, value ?? string.Empty);
        }

        [DataMember]
        public string IconUrl
        {
            get => this.iconUrl;
            set => this.SetPropertyValue(ref this.iconUrl, value ?? string.Empty);
        }

        [DataMember]
        public string Authors
        {
            get => this.authors;
            set => this.SetPropertyValue(ref this.authors, value ?? string.Empty);
        }

        [DataMember]
        public string Version
        {
            get => this.version;
            set => this.SetPropertyValue(ref this.version, value ?? string.Empty);
        }

        [DataMember]
        public string VersionRegistrationUrl
        {
            get => this.versionRegistrationUrl;
            set => this.SetPropertyValue(ref this.versionRegistrationUrl, value ?? string.Empty);
        }
    }
}
