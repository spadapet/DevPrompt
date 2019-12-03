using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DevPrompt.Plugins
{
    [DebuggerDisplay("{PluginInfo}")]
    internal sealed class PluginSource
    {
        public Assembly[] Assemblies { get; }
        public PluginSourceType PluginType { get; }
        public InstalledPluginInfo PluginInfo { get; }

        public PluginSource(PluginSourceType pluginType, IEnumerable<Assembly> assemblies, InstalledPluginInfo pluginInfo)
        {
            Debug.Assert(assemblies != null && pluginInfo != null);

            this.PluginType = pluginType;
            this.Assemblies = assemblies.ToArray();
            this.PluginInfo = pluginInfo;
        }
    }
}
