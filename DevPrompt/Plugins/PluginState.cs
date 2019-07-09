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
        public IEnumerable<Assembly> PluginAssemblies => this.pluginAssemblies ?? Enumerable.Empty<Assembly>();
        public IEnumerable<Api.IAppListener> AppListeners => this.appListeners ?? Enumerable.Empty<Api.IAppListener>();
        public IEnumerable<IProcessListener> ProcessListeners => this.processListeners ?? Enumerable.Empty<IProcessListener>();
        public IEnumerable<Api.IMenuItemProvider> MenuItemProviders => this.menuItemProviders ?? Enumerable.Empty<Api.IMenuItemProvider>();
        public IEnumerable<Api.IWorkspaceProvider> WorkspaceProviders => this.workspaceProviders ?? Enumerable.Empty<Api.IWorkspaceProvider>();

        private App app;
        private List<Assembly> pluginAssemblies;
        private CompositionHost compositionHost;
        private Api.IAppListener[] appListeners;
        private IProcessListener[] processListeners;
        private Api.IMenuItemProvider[] menuItemProviders;
        private Api.IWorkspaceProvider[] workspaceProviders;

#if NET_FRAMEWORK
        private const string DllSuffix = ".Plugin.dll";
#else
        private const string DllSuffix = ".NetCorePlugin.dll";
#endif

        public PluginState(App app)
        {
            this.app = app;
            this.pluginAssemblies = new List<Assembly>();
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
                    conventions.ForType<IProcessCache>().Shared();
                    conventions.ForType<IProcessListener>().Shared();

                    CompositionHost host = new ContainerConfiguration()
                        .WithDefaultConventions(conventions)
                        .WithAssemblies(this.GetPluginAssemblies(localAssemblies), conventions)
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

            this.pluginAssemblies.AddRange(localAssemblies);

            return compositionHost;
        }

        private IEnumerable<Assembly> GetPluginAssemblies(ICollection<Assembly> appAssemblies)
        {
            List<Assembly> assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };

            foreach (PluginDirectorySettings pluginDir in this.app.Settings.PluginDirectories)
            {
                if (pluginDir.Enabled)
                {
                    assemblies.AddRange(this.GetPluginAssemblies(pluginDir.ExpandedDirectory, pluginDir.Recurse));
                }
            }

            // User can force specific plugins from the command line
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i + 1 < args.Length; i++)
            {
                if (args[i] == "/plugin")
                {
                    string file = args[++i];
                    if (file.EndsWith(PluginState.DllSuffix, StringComparison.OrdinalIgnoreCase) && File.Exists(file))
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
                }
            }

            foreach (Assembly assembly in assemblies.Distinct())
            {
                appAssemblies?.Add(assembly);
                yield return assembly;
            }
        }

        private IEnumerable<Assembly> GetPluginAssemblies(string path, bool recursive)
        {
            List<Assembly> assemblies = new List<Assembly>();
            IEnumerable<string> files = Enumerable.Empty<string>();

            try
            {
                if (Directory.Exists(path))
                {
                    files = Directory.EnumerateFiles(path, "*" + PluginState.DllSuffix, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToArray();
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
