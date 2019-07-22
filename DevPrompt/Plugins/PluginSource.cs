using System.Reflection;

namespace DevPrompt.Plugins
{
    internal class PluginSource
    {
        public Assembly Assembly { get; }
        public PluginSourceType PluginType { get; }
        public string PluginId { get; }
        public string PluginVersion { get; }

        public PluginSource(PluginSourceType pluginType, Assembly assembly, string pluginId = null, string pluginVersion = null)
        {
            AssemblyName assemblyName = assembly.GetName();

            this.PluginType = pluginType;
            this.Assembly = assembly;
            this.PluginId = !string.IsNullOrEmpty(pluginId) ? pluginId : assemblyName.FullName;
            this.PluginVersion = !string.IsNullOrEmpty(pluginVersion) ? pluginVersion : assemblyName.Version.ToString();
        }
    }
}
