using DevPrompt.Settings;
using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Loads all plugin DLLs
    /// </summary>
    internal static class PluginUtility
    {
#if NET_FRAMEWORK
        private const string DllSuffix = ".Plugin.dll";
#else
        private const string DllSuffix = ".NetCorePlugin.dll";
#endif

        public static async Task<CompositionHost> CreatePluginHost(App app, ICollection<Assembly> appAssemblies)
        {
            List<Assembly> localAssemblies = new List<Assembly>();

            CompositionHost compositionHost = await Task.Run(() =>
            {
                try
                {
                    ConventionBuilder conventions = new ConventionBuilder();
                    conventions.ForType<Api.IApp>().Shared();
                    conventions.ForType<Api.IAppListener>().Shared();
                    conventions.ForType<Api.IAppSettings>().Shared();
                    conventions.ForType<Api.IWindow>().Shared();
                    conventions.ForType<Api.IProcessListener>().Shared();
                    conventions.ForType<Interop.IProcessCache>().Shared();
                    conventions.ForType<Interop.IProcessListener>().Shared();

                    CompositionHost host = new ContainerConfiguration()
                        .WithDefaultConventions(conventions)
                        .WithAssemblies(PluginUtility.GetPluginAssemblies(localAssemblies), conventions)
                        .WithProvider(new ExportProvider(app))
                        .CreateContainer();

                    return host;
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.Message, ex.StackTrace);
                    return null;
                }
            });

            if (appAssemblies != null)
            {
                foreach (Assembly assembly in localAssemblies)
                {
                    appAssemblies.Add(assembly);
                }
            }

            return compositionHost;
        }

        private static IEnumerable<Assembly> GetPluginAssemblies(ICollection<Assembly> appAssemblies)
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

            foreach (Assembly assembly in assemblies.Distinct())
            {
                appAssemblies?.Add(assembly);
                yield return assembly;
            }
        }

        private static IEnumerable<Assembly> GetPluginAssemblies(string path)
        {
            List<Assembly> assemblies = new List<Assembly>();
            IEnumerable<string> files = Enumerable.Empty<string>();

            try
            {
                if (Directory.Exists(path))
                {
                    files = Directory.EnumerateFiles(path, "*" + PluginUtility.DllSuffix, SearchOption.AllDirectories).ToArray();
                }
            }
            catch
            {
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
