using DevPrompt.Settings;
using DevPrompt.UI.Plugins;
using DevPrompt.Utility.NuGet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the plugins dialog
    /// </summary>
    internal class PluginsDialogVM : Api.PropertyNotifier, IDisposable
    {
        public AppSettings Settings { get; }
        public IList<IPluginVM> AvailablePlugins => this.availablePlugins;

        private MainWindow window;
        private PluginsDialog dialog;
        private PluginsTabVM[] tabs;
        private PluginsTabVM activeTab;
        private CancellationTokenSource cancelSource;
        private ObservableCollection<IPluginVM> availablePlugins;

        public PluginsDialogVM()
            : this(null, null, null, PluginsTabType.Default)
        {
        }

        public PluginsDialogVM(MainWindow window, PluginsDialog dialog, AppSettings settings, PluginsTabType activeTabType)
        {
            this.window = window;
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
                using (NuGetServiceIndex nuget = await NuGetServiceIndex.Create(this.window.App.HttpClient))
                {
                    foreach (NuGetSearchResult result in await nuget.Search(NuGetServiceIndex.PluginSearchQuery))
                    {
                        NuGetSearchResultVersion latestVersion = result.versions.FirstOrDefault(v => v.version == result.version);
                        if (result.type == "Package" && !string.IsNullOrEmpty(latestVersion.idUrl))
                        {
                            NuGetPluginSettings settings = new NuGetPluginSettings()
                            {
                                Authors = string.Join(", ", result.authors),
                                Description = result.description,
                                Enabled = true,
                                Id = result.id,
                                Path = string.Empty,
                                ProjectUrl = result.projectUrl,
                                Title = result.title,
                                Version = result.version,
                                VersionRegistrationUrl = latestVersion.idUrl,
                            };

                            this.availablePlugins.Add(new NuGetPluginVM(settings));
                        }
                    }
                }
            }
            catch
            {
                // TODO: Show error in dialog
            }
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
    }
}
