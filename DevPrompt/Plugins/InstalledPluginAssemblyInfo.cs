using DevPrompt.ProcessWorkspace.Utility;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Caches information about an installed assembly in a plugin
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{FullName}")]
    internal class InstalledPluginAssemblyInfo : PropertyNotifier
    {
        private AssemblyName assemblyName;
        private string path;
        private bool isContainer;

        public InstalledPluginAssemblyInfo()
        {
            this.Initialize();
        }

        public InstalledPluginAssemblyInfo(InstalledPluginAssemblyInfo copyFrom)
        {
            this.Initialize();
            this.CopyFrom(copyFrom);
        }

        public void CopyFrom(InstalledPluginAssemblyInfo copyFrom)
        {
            this.AssemblyName = copyFrom.AssemblyName;
            this.Path = copyFrom.Path;
            this.IsContainer = copyFrom.IsContainer;
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.path = string.Empty;
        }

        public InstalledPluginAssemblyInfo Clone()
        {
            return new InstalledPluginAssemblyInfo(this);
        }

        public AssemblyName AssemblyName
        {
            get => this.assemblyName;
            set
            {
                if (this.SetPropertyValue(ref this.assemblyName, value))
                {
                    this.OnPropertyChanged(nameof(this.FullName));
                }
            }
        }

        [DataMember]
        public string FullName
        {
            get => this.assemblyName?.FullName ?? string.Empty;
            set
            {
                value = value ?? string.Empty;
                if (this.FullName != value)
                {
                    try
                    {
                        this.AssemblyName = new AssemblyName(value);
                    }
                    catch
                    {
                        this.AssemblyName = null;
                    }
                }
            }
        }

        [DataMember]
        public string Path
        {
            get => this.path;
            set => this.SetPropertyValue(ref this.path, value ?? string.Empty);
        }

        [DataMember]
        public bool IsContainer
        {
            get => this.isContainer;
            set => this.SetPropertyValue(ref this.isContainer, value);
        }
    }
}
