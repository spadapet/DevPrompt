using DevPrompt.Interop;
using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DevPrompt.Plugins
{
    internal class PluginState : IDisposable
    {
        public IProcessCache ProcessCache { get; private set; }
        public bool Initialized => this.ProcessCache != null;
        public IEnumerable<PluginSource> Plugins => this.plugins;
        public IEnumerable<Api.IAppListener> AppListeners => this.appListeners ?? Enumerable.Empty<Api.IAppListener>();
        public IEnumerable<IProcessListener> ProcessListeners => this.processListeners ?? Enumerable.Empty<IProcessListener>();
        public IEnumerable<Api.ICommandProvider> CommandProviders => this.commandProviders ?? Enumerable.Empty<Api.ICommandProvider>();
        public IEnumerable<Api.IWorkspaceProvider> WorkspaceProviders => this.workspaceProviders ?? Enumerable.Empty<Api.IWorkspaceProvider>();

        private App app;
        private List<PluginSource> plugins;
        private CompositionHost compositionHost;
        private Api.IAppListener[] appListeners;
        private IProcessListener[] processListeners;
        private Api.ICommandProvider[] commandProviders;
        private Api.IWorkspaceProvider[] workspaceProviders;

#if NET_FRAMEWORK
        public const string DllSuffix = ".Plugin.dll";
#else
        public const string DllSuffix = ".NetCorePlugin.dll";
#endif

        public PluginState(App app)
        {
            this.app = app;
            this.plugins = new List<PluginSource>();
        }

        public void Dispose()
        {
            this.compositionHost?.Dispose();
        }

        public async Task Initialize(bool firstStartup)
        {
            if (firstStartup)
            {
                await this.CleanNuGetPlugins();
            }

            this.compositionHost = await this.CreatePluginHost();

            if (this.compositionHost != null)
            {
                this.ProcessCache = this.compositionHost.GetExport<IProcessCache>();
                this.appListeners = this.GetOrderedExports<Api.IAppListener>().ToArray();
                this.processListeners = this.GetOrderedExports<IProcessListener>().ToArray();
                this.commandProviders = this.GetOrderedExports<Api.ICommandProvider>().ToArray();
                this.workspaceProviders = this.GetOrderedExports<Api.IWorkspaceProvider>().ToArray();
            }
            else
            {
                // Plugin support is broken (maybe missing DLLs) but the app should work anyway
                this.ProcessCache = new NativeProcessCache();
            }
        }

        public IEnumerable<Assembly> AllPluginAssemblies
        {
            get
            {
                foreach (PluginSource plugin in this.Plugins)
                {
                    foreach (Assembly assembly in plugin.Assemblies)
                    {
                        yield return assembly;
                    }
                }
            }
        }

        private async Task CleanNuGetPlugins()
        {
            NuGetPluginSettings[] nugetPlugins = this.app.Settings.NuGetPlugins.Select(p => p.Clone()).ToArray();

            await Task.Run(() =>
            {
                Dictionary<string, NuGetPluginSettings> pluginMap = nugetPlugins.ToDictionary(p => p.Id, p => p);

                // Detect installed plugins that were manually deleted
                foreach (NuGetPluginSettings plugin in nugetPlugins)
                {
                    if (plugin.IsInstalled && !Directory.Exists(plugin.InstalledVersionPath))
                    {
                        plugin.InstalledInfo = null;
                    }
                }

                try
                {
                    if (Directory.Exists(AppSettings.DefaultNuGetPath))
                    {
                        foreach (string path in Directory.GetDirectories(AppSettings.DefaultNuGetPath, "*", SearchOption.TopDirectoryOnly))
                        {
                            string id = Path.GetFileName(path);
                            if (pluginMap.TryGetValue(id, out NuGetPluginSettings plugin) && plugin.IsInstalled)
                            {
                                foreach (string versionPath in Directory.GetDirectories(plugin.InstalledRootPath, "*", SearchOption.TopDirectoryOnly))
                                {
                                    string version = Path.GetFileName(versionPath);
                                    if (!string.Equals(version, plugin.InstalledVersion, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // This version isn't installed, delete just this one
                                        Directory.Delete(versionPath, recursive: true);
                                    }
                                }

                                // Deleted the last version, so delete its root
                                if (!Directory.EnumerateFileSystemEntries(plugin.InstalledRootPath).Any())
                                {
                                    Directory.Delete(plugin.InstalledRootPath, recursive: true);
                                }
                            }
                            else
                            {
                                // Plugin isn't installed at all, delete its root
                                Directory.Delete(path, recursive: true);
                            }
                        }

                        // Deleted the last plugin
                        if (!Directory.EnumerateFileSystemEntries(AppSettings.DefaultNuGetPath).Any())
                        {
                            Directory.Delete(AppSettings.DefaultNuGetPath, recursive: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Fail($"CleanNuGetPlugins failed: {ex}");
                }
            });

            // Copy back plugin info, in case some installed plugins were detected as not installed
            Debug.Assert(nugetPlugins.Length == this.app.Settings.NuGetPlugins.Count);

            for (int i = 0; i < nugetPlugins.Length && i < this.app.Settings.NuGetPlugins.Count; i++)
            {
                NuGetPluginSettings appPlugin = this.app.Settings.NuGetPlugins[i];
                NuGetPluginSettings updatedPlugin = nugetPlugins[i];
                Debug.Assert(appPlugin.Id == updatedPlugin.Id);

                appPlugin.CopyFrom(updatedPlugin);
            }
        }

        private IEnumerable<T> GetOrderedExports<T>()
        {
            return this.compositionHost.GetExports<ExportFactory<T, Api.OrderAttribute>>()
                .OrderBy(i => i.Metadata.Order)
                .Select(i => i.CreateExport().Value);
        }

        private async Task<CompositionHost> CreatePluginHost()
        {
            List<PluginSource> plugins = new List<PluginSource>();
            PluginDirectorySettings[] pluginDirectories = this.app.Settings.PluginDirectories.Select(p => p.Clone()).ToArray();
            NuGetPluginSettings[] nugetPlugins = this.app.Settings.NuGetPlugins.Select(p => p.Clone()).ToArray();

            CompositionHost compositionHost = await Task.Run(async () =>
            {
                try
                {
                    ConventionBuilder conventions = new ConventionBuilder();
                    conventions.ForType<Api.IApp>().Shared();
                    conventions.ForType<Api.IAppListener>().Shared();
                    conventions.ForType<Api.IAppSettings>().Shared();
                    conventions.ForType<Api.IProcessListener>().Shared();
                    conventions.ForType<IProcessCache>().Shared();
                    conventions.ForType<IProcessListener>().Shared();

                    plugins.AddRange(await PluginState.LoadPlugins(pluginDirectories, nugetPlugins));

                    List<Assembly> pluginAssemblies = new List<Assembly>(plugins.Count);
                    foreach (PluginSource plugin in plugins)
                    {
                        pluginAssemblies.AddRange(plugin.Assemblies);
                    }

                    CompositionHost host = new ContainerConfiguration()
                        .WithDefaultConventions(conventions)
                        .WithAssemblies(pluginAssemblies, conventions)
                        .WithProvider(new ExportProvider(this.app))
                        .CreateContainer();

                    return host;
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.Message, ex.StackTrace);
                    return null;
                }
            });

            this.plugins.AddRange(plugins);

            return compositionHost;
        }

        private static PluginSource CreatePluginSource(Assembly assembly)
        {
            AssemblyName assemblyName = assembly.GetName();

            InstalledPluginInfo pluginInfo = new InstalledPluginInfo()
            {
                Id = assemblyName.Name,
                Version = assemblyName.Version.ToString(),
                RootPath = Path.GetDirectoryName(assembly.Location),
            };

            List<InstalledPluginAssemblyInfo> assemblyList = new List<InstalledPluginAssemblyInfo>()
            {
                new InstalledPluginAssemblyInfo()
                {
                    AssemblyName = assemblyName,
                    Path = assembly.Location,
                    IsContainer = true,
                }
            };

            pluginInfo.Assemblies[assemblyName.Name] = assemblyList;

            return new PluginSource(PluginSourceType.BuiltIn, new Assembly[] { assembly }, pluginInfo);
        }

        private static async Task<PluginSource> TryCreateDirectoryPluginSource(PluginSourceType pluginType, string root, InstalledPluginInfo pluginInfo = null)
        {
            if (Directory.Exists(root))
            {
                if (pluginInfo == null)
                {
                    pluginInfo = new InstalledPluginInfo()
                    {
                        RootPath = root,
                    };

                    try
                    {
                        await pluginInfo.GatherAssemblyInfo();
                    }
                    catch
                    {
                        return null;
                    }
                }

                List<Assembly> assemblies = new List<Assembly>();
                foreach (string file in pluginInfo.PluginContainerFiles)
                {
                    assemblies.AddRange(PluginState.LoadAssemblies(root, recursive: false));
                }

                if (assemblies.Count > 0)
                {
                    AssemblyName name = assemblies[0].GetName();
                    pluginInfo.Id = name.Name;
                    pluginInfo.Version = name.Version.ToString();

                    return new PluginSource(pluginType, assemblies, pluginInfo);
                }
            }

            return null;
        }

        private class PluginSourceComparer : IEqualityComparer<PluginSource>
        {
            public bool Equals(PluginSource x, PluginSource y) => x.PluginInfo.RootPath.Equals(y.PluginInfo.RootPath, StringComparison.OrdinalIgnoreCase);
            public int GetHashCode(PluginSource obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PluginInfo.RootPath);
        }

        private static async Task<IEnumerable<PluginSource>> LoadPlugins(IEnumerable<PluginDirectorySettings> pluginDirectories, IEnumerable<NuGetPluginSettings> nugetPlugins)
        {
            // The app itself is always a plugin
            PluginSource[] defaultPlugins = new PluginSource[]
            {
                PluginState.CreatePluginSource(Assembly.GetExecutingAssembly()),
                PluginState.CreatePluginSource(typeof(PropertyNotifier).Assembly),
            };

            HashSet<PluginSource> plugins = new HashSet<PluginSource>(new PluginSourceComparer());

            // Users can force specific plugins from the command line
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i + 1 < args.Length; i++)
            {
                if (args[i] == "/plugin")
                {
                    string root = args[++i];
                    if (root.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        root = Path.GetDirectoryName(root);
                    }

                    PluginSource pluginSource = await PluginState.TryCreateDirectoryPluginSource(PluginSourceType.CommandLine, root);
                    if (pluginSource != null)
                    {
                        plugins.Add(pluginSource);
                    }
                }
            }

            // The user can block all other plugins from loading
            foreach (string arg in args)
            {
                if (arg == "/noplugins")
                {
                    return plugins;
                }
            }

            // Load from all plugin directories
            foreach (PluginDirectorySettings pluginDir in pluginDirectories)
            {
                if (pluginDir.Enabled)
                {
                    PluginSource pluginSource = await PluginState.TryCreateDirectoryPluginSource(PluginSourceType.Directory, pluginDir.ExpandedDirectory);
                    if (pluginSource != null)
                    {
                        plugins.Add(pluginSource);
                    }
                }
            }

            // Load from NuGet packages
            foreach (NuGetPluginSettings nuget in nugetPlugins)
            {
                if (nuget.IsInstalled)
                {
                    PluginSource pluginSource = await PluginState.TryCreateDirectoryPluginSource(PluginSourceType.NuGet, nuget.InstalledVersionPath, nuget.InstalledInfo);
                    if (pluginSource != null)
                    {
                        plugins.Add(pluginSource);
                    }
                }
            }

            return Enumerable.Concat(defaultPlugins, plugins);
        }

        private static IEnumerable<Assembly> LoadAssemblies(string path, bool recursive)
        {
            List<Assembly> assemblies = new List<Assembly>();
            IEnumerable<string> files = Enumerable.Empty<string>();

            try
            {
                if (File.Exists(path))
                {
                    files = new string[] { path };
                }
                else if (Directory.Exists(path))
                {
                    files = Directory.EnumerateFiles(path, "*" + PluginState.DllSuffix, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToArray();
                }
            }
            catch
            {
                // ignore failure
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
