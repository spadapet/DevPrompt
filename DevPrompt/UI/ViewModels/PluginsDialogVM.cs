using DevPrompt.Api;
using DevPrompt.Settings;
using DevPrompt.UI.Plugins;
using DevPrompt.Utility.NuGet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the plugins dialog
    /// </summary>
    internal class PluginsDialogVM : Api.PropertyNotifier, IDisposable
    {
        public AppSettings Settings { get; }
        public MainWindow Window { get; }
        public IList<IPluginVM> AvailablePlugins => this.availablePlugins;

        private PluginsDialog dialog;
        private PluginsTabVM[] tabs;
        private PluginsTabVM activeTab;
        private CancellationTokenSource cancelSource;
        private ObservableCollection<IPluginVM> availablePlugins;
        private IPluginVM currentPlugin;

        public PluginsDialogVM()
            : this(null, null, null, PluginsTabType.Default)
        {
        }

        public PluginsDialogVM(MainWindow window, PluginsDialog dialog, AppSettings settings, PluginsTabType activeTabType)
        {
            this.Window = window;
            this.dialog = dialog;
            this.cancelSource = new CancellationTokenSource();
            this.availablePlugins = new ObservableCollection<IPluginVM>();
            this.Settings = settings?.Clone();

            this.tabs = new PluginsTabVM[]
                {
                    new PluginsTabVM(this, PluginsTabType.PluginInstalled),
                    new PluginsTabVM(this, PluginsTabType.PluginAvailable),
                    new PluginsTabVM(this, PluginsTabType.PluginUpdates),
                };

            this.ActiveTabType = activeTabType;

            this.Initialize();
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
            this.cancelSource.Dispose();
        }

        public async void Initialize()
        {
            try
            {
                using (NuGetServiceIndex nuget = await NuGetServiceIndex.Create(this.Window.App.HttpClient))
                {
                    foreach (NuGetSearchResult result in await nuget.Search(NuGetServiceIndex.PluginSearchQuery))
                    {
                        NuGetSearchResultVersion latestVersion = result.versions.FirstOrDefault(v => v.version == result.version);
                        if (result.type == "Package" && !string.IsNullOrEmpty(latestVersion.idUrl))
                        {
                            this.AddAvailablePlugin(result, latestVersion);
                        }
                    }
                }
            }
            catch
            {
                // TODO: Show error in dialog
            }
        }

        private void AddAvailablePlugin(NuGetSearchResult plugin, NuGetSearchResultVersion version)
        {
            NuGetPluginSettings settings = this.Window.App.Settings.NuGetPlugins.FirstOrDefault(s => s.Id == plugin.id);
            if (settings != null)
            {
                settings = settings.Clone();
            }
            else
            {
                settings = new NuGetPluginSettings()
                {
                    Authors = string.Join(", ", plugin.authors),
                    Description = plugin.description,
                    Summary = plugin.summary,
                    Enabled = true,
                    Id = plugin.id,
                    Path = string.Empty,
                    IconUrl = plugin.iconUrl,
                    ProjectUrl = plugin.projectUrl,
                    Title = plugin.title,
                    Version = plugin.version,
                    VersionRegistrationUrl = version.idUrl,
                };
            }

            this.availablePlugins.Add(new NuGetPluginVM(this.Window, settings, version.version, version.idUrl));
        }

        public IList<PluginsTabVM> Tabs => this.tabs;

        public PluginsTabType ActiveTabType
        {
            get => this.activeTab.TabType;

            set
            {
                foreach (PluginsTabVM tab in this.Tabs)
                {
                    if (tab.TabType == value)
                    {
                        this.ActiveTab = tab;
                        break;
                    }
                }
            }
        }

        public PluginsTabVM ActiveTab
        {
            get => this.activeTab;

            set
            {
                if (this.SetPropertyValue(ref this.activeTab, value ?? this.Tabs.First()))
                {
                    this.OnPropertyChanged(nameof(this.ActiveTabType));
                }
            }
        }

        public IPluginVM CurrentPlugin
        {
            get => this.currentPlugin;
            set => this.SetPropertyValue(ref this.currentPlugin, value);
        }

        public ICommand InstallPluginCommand => new DelegateCommand(async (object obj) =>
        {
            if (obj is IPluginVM plugin)
            {
                using (this.Window.ViewModel.BeginLoading(this.cancelSource.Cancel, string.Empty))
                {
                    try
                    {
                        await plugin.Install(this.cancelSource.Token);
                    }
                    catch
                    {
                        // TODO: Deal with failure
                    }
                }
            }
        });

        public ICommand UninstallPluginCommand => new DelegateCommand(async (object obj) =>
        {
            if (obj is IPluginVM plugin)
            {
                using (this.Window.ViewModel.BeginLoading(this.cancelSource.Cancel, string.Empty))
                {
                    try
                    {
                        await plugin.Uninstall(this.cancelSource.Token);
                    }
                    catch
                    {
                        // TODO: Deal with failure
                    }
                }
            }
        });
    }
}
