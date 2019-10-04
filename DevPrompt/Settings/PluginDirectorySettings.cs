using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// A single plugin directory customizable by the user
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Directory}")]
    internal class PluginDirectorySettings : PropertyNotifier
    {
        private string directory;
        private bool enabled;
        private bool readOnly;

        public PluginDirectorySettings()
        {
            this.directory = ".";
            this.enabled = true;
        }

        public PluginDirectorySettings(PluginDirectorySettings copyFrom)
        {
            this.directory = copyFrom.directory;
            this.enabled = copyFrom.enabled;
            this.readOnly = copyFrom.readOnly;
        }

        public PluginDirectorySettings Clone()
        {
            return new PluginDirectorySettings(this);
        }

        [DataMember]
        public string Directory
        {
            get => this.directory;

            set
            {
                if (this.SetPropertyValue(ref this.directory, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.ExpandedDirectory));
                }
            }
        }

        [DataMember]
        public bool Enabled
        {
            get => this.enabled;
            set => this.SetPropertyValue(ref this.enabled, value);
        }

        [DataMember]
        public bool ReadOnly
        {
            get => this.readOnly;
            set => this.SetPropertyValue(ref this.readOnly, value);
        }

        public string ExpandedDirectory
        {
            get
            {
                try
                {
                    string startDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string directory = Environment.ExpandEnvironmentVariables(this.Directory);
                    directory = Path.Combine(startDirectory, directory);
                    directory = Path.GetFullPath(directory);

                    return directory;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
    }
}
