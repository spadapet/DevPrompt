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
    internal class PluginDirectorySettings : Api.PropertyNotifier, IEquatable<PluginDirectorySettings>
    {
        private string directory;
        private bool recurse;
        private bool enabled;

        public PluginDirectorySettings()
        {
            this.directory = ".";
            this.recurse = false;
            this.enabled = true;
        }

        public PluginDirectorySettings(PluginDirectorySettings copyFrom)
        {
            this.directory = copyFrom.directory;
            this.recurse = copyFrom.recurse;
            this.enabled = copyFrom.enabled;
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
        public bool Recurse
        {
            get => this.recurse;
            set => this.SetPropertyValue(ref this.recurse, value);
        }

        [DataMember]
        public bool Enabled
        {
            get => this.enabled;
            set => this.SetPropertyValue(ref this.enabled, value);
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

        public override int GetHashCode()
        {
            return this.directory.GetHashCode() ^ this.recurse.GetHashCode() ^ (this.enabled.GetHashCode() << 1);
        }

        public override bool Equals(object obj)
        {
            return obj is PluginDirectorySettings other && this.Equals(other);
        }

        public bool Equals(PluginDirectorySettings other)
        {
            return this.directory == other.directory && this.recurse == other.recurse && this.enabled == other.enabled;
        }
    }
}
