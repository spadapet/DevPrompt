using System.Reflection;

namespace DevPrompt.Plugins
{
    internal class PluginSource
    {
        public PluginSourceType PluginType { get; }
        public string Id { get; }
        public string Path { get; }
        public Assembly Assembly { get; }

        public PluginSource(PluginSourceType pluginType, string id, string path, Assembly assembly)
        {
            this.PluginType = pluginType;
            this.Id = id;
            this.Path = path;
            this.Assembly = assembly;
        }
    }
}
