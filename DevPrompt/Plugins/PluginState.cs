using DevPrompt.Interop;
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
        public IEnumerable<Api.IMenuItemProvider> MenuItemProviders => this.menuItemProviders ?? Enumerable.Empty<Api.IMenuItemProvider>();
        public IEnumerable<Api.IWorkspaceProvider> WorkspaceProviders => this.workspaceProviders ?? Enumerable.Empty<Api.IWorkspaceProvider>();

        private App app;
        private List<PluginSource> plugins;
        private CompositionHost compositionHost;
        private Api.IAppListener[] appListeners;
        private IProcessListener[] processListeners;
        private Api.IMenuItemProvider[] menuItemProviders;
        private Api.IWorkspaceProvider[] workspaceProviders;

#if NET_FRAMEWORK
        private const string DllSuffix = ".Plugin.dll";
        private const string NuGetPlatformDir = "net40";
#else
        private const string DllSuffix = ".NetCorePlugin.dll";
        private const string NuGetPlatformDir = "net40";
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

        public async Task Initialize()
        {
            this.compositionHost = await this.CreatePluginHost();

            if (this.compositionHost != null)
            {
                this.ProcessCache = this.compositionHost.GetExport<IProcessCache>();
                this.appListeners = this.GetOrderedExports<Api.IAppListener>().ToArray();
                this.processListeners = this.GetOrderedExports<IProcessListener>().ToArray();
                this.menuItemProviders = this.GetOrderedExports<Api.IMenuItemProvider>().ToArray();
                this.workspaceProviders = this.GetOrderedExports<Api.IWorkspaceProvider>().ToArray();
            }
            else
            {
                // Plugin support is broken (maybe missing DLLs) but the app should work anyway
                this.ProcessCache = new NativeProcessCache();
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
            PluginDirectorySettings[] pluginDirectories = this.app.Settings.PluginDirectories.ToArray();
            NuGetPluginSettings[] nugetPlugins = this.app.Settings.NuGetPlugins.ToArray();

            CompositionHost compositionHost = await Task.Run(() =>
            {
                try
                {
                    ConventionBuilder conventions = new ConventionBuilder();
                    conventions.ForType<Api.IApp>().Shared();
                    conventions.ForType<Api.IAppListener>().Shared();
                    conventions.ForType<Api.IAppSettings>().Shared();
                    conventions.ForType<Api.IHttpClient>().Shared();
                    conventions.ForType<Api.IProcessListener>().Shared();
                    conventions.ForType<IProcessCache>().Shared();
                    conventions.ForType<IProcessListener>().Shared();

                    plugins.AddRange(PluginState.LoadPlugins(pluginDirectories, nugetPlugins));

                    CompositionHost host = new ContainerConfiguration()
                        .WithDefaultConventions(conventions)
                        .WithAssemblies(plugins.Select(p => p.Assembly), conventions)
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

        private class PluginSourceComparer : IEqualityComparer<PluginSource>
        {
            public bool Equals(PluginSource x, PluginSource y) => x.Assembly == y.Assembly;
            public int GetHashCode(PluginSource obj) => obj.Assembly.GetHashCode();
        }

        private static IEnumerable<PluginSource> LoadPlugins(IEnumerable<PluginDirectorySettings> pluginDirectories, IEnumerable<NuGetPluginSettings> nugetPlugins)
        {
            // The app itself is always a plugin
            HashSet<PluginSource> plugins = new HashSet<PluginSource>(new PluginSourceComparer())
            {
                new PluginSource(PluginSourceType.BuiltIn, Assembly.GetExecutingAssembly()),
            };

            // Users can force specific plugins from the command line
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i + 1 < args.Length; i++)
            {
                if (args[i] == "/plugin")
                {
                    string file = args[++i];
                    foreach (Assembly assembly in PluginState.LoadAssemblies(file, recursive: false))
                    {
                        plugins.Add(new PluginSource(PluginSourceType.CommandLine, assembly));
                    }
                }
            }

            // Load from all plugin directories
            foreach (PluginDirectorySettings pluginDir in pluginDirectories)
            {
                if (pluginDir.Enabled)
                {
                    foreach (Assembly assembly in PluginState.LoadAssemblies(pluginDir.ExpandedDirectory, pluginDir.Recurse))
                    {
                        plugins.Add(new PluginSource(PluginSourceType.Directory, assembly));
                    }
                }
            }

            // Load from NuGet packages
            foreach (NuGetPluginSettings nuget in nugetPlugins)
            {
                if (nuget.Enabled)
                {
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", nuget.Id, nuget.Version, "tools", PluginState.NuGetPlatformDir);
                    foreach (Assembly assembly in PluginState.LoadAssemblies(path, recursive: false))
                    {
                        plugins.Add(new PluginSource(PluginSourceType.NuGet, assembly));
                    }
                }
            }

            return plugins;
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
