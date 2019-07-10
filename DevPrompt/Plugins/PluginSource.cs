using System.Reflection;

namespace DevPrompt.Plugins
{
    internal class PluginSource
    {
        public Assembly Assembly { get; }
        public PluginSourceType PluginType { get; }

        public PluginSource(PluginSourceType pluginType, Assembly assembly)
        {
            this.PluginType = pluginType;
            this.Assembly = assembly;
        }
    }
}
