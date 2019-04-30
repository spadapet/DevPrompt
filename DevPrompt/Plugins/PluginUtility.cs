using DevPrompt.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DevPrompt.Plugins
{
    internal static class PluginUtility
    {
        public static IEnumerable<Assembly> GetPluginAssemblies()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            List<Assembly> assemblies = new List<Assembly>();
            assemblies.Add(thisAssembly);

            string[] paths = new string[]
                {
                    Path.GetDirectoryName(thisAssembly.Location),
                    AppSettings.AppDataPath + ".Plugins"
                };

            foreach (string path in paths)
            {
                assemblies.AddRange(PluginUtility.GetPluginAssemblies(path));
            }

            return assemblies;
        }

        private static IEnumerable<Assembly> GetPluginAssemblies(string path)
        {
            List<Assembly> assemblies = new List<Assembly>();
            IEnumerable<string> files;

#if NET_FRAMEWORK
            string filter = "*.Plugin.dll";
#else
            string filter = "*.NetCorePlugin.dll";
#endif
            try
            {
                files = Directory.EnumerateFiles(path, filter, SearchOption.TopDirectoryOnly).ToArray();
            }
            catch
            {
                files = Enumerable.Empty<string>();
            }

            foreach (string file in files)
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(file));
                }
                catch
                {
                    // ignore failure
                }
            }

            return assemblies;
        }
    }
}
