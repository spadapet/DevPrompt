using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.Utility;
using DevPrompt.Utility.NuGet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    internal class PluginSettingsControlVM : PropertyNotifier, IDisposable
    {
        public IList<IPluginVM> Plugins => this.plugins;
        public IList<PluginSortVM> Sorts => this.sorts;
        public MainWindow Window => this.settingsVM.Window;
        public App App => this.settingsVM.App;
        public AppSettings AppSettings => this.settingsVM.Settings;
        public Api.IProgressBar ProgressBar => this.settingsVM.ProgressBar;
        public Api.IInfoBar InfoBar => this.settingsVM.Dialog.IsLoaded ? this.settingsVM.InfoBar : this.Window.infoBar;

        private readonly SettingsDialogVM settingsVM;
        private readonly ObservableCollection<IPluginVM> plugins;
        private readonly ObservableCollection<PluginSortVM> sorts;
        private PluginSortVM sort;
        private IPluginVM currentPlugin;
        private bool isBusy;

        public PluginSettingsControlVM()
            : this(new SettingsDialogVM())
        {
        }

        public PluginSettingsControlVM(SettingsDialogVM settingsVM)
        {
            this.settingsVM = settingsVM;
            this.plugins = new ObservableCollection<IPluginVM>(this.AppSettings.NuGetPlugins.Select(p => new NuGetPluginVM(this.App, this.AppSettings, p)));

            this.sorts = new ObservableCollection<PluginSortVM>()
            {
                new PluginSortVM(Resources.PluginDialog_Sort_Installed, new PluginSortInstalled()),
                new PluginSortVM(Resources.PluginDialog_Sort_MostRecent, new PluginSortMostRecent()),
                new PluginSortVM(Resources.PluginDialog_Sort_Name, new PluginSortName()),
            };

            this.Sort = this.sorts[0];

            this.Refresh();
        }

        public void Dispose()
        {
            foreach (IPluginVM plugin in this.plugins)
            {
                plugin.Dispose();
            }
        }

        public async void Refresh()
        {
            List<NuGetPluginVM> initialPlugins = this.plugins.OfType<NuGetPluginVM>().ToList();
            List<NuGetPluginVM> finalPlugins = new List<NuGetPluginVM>(initialPlugins.Count);

            try
            {
                using (this.BeginBusy(out CancellationTokenSource cancelSource))
                using (this.ProgressBar.Begin(cancelSource.Cancel, Resources.Plugins_FetchingFromNuGet))
                using (NuGetServiceIndex nuget = await NuGetServiceIndex.Create(this.Window.App.HttpClient, cancelSource.Token))
                {
                    foreach (NuGetSearchResult result in await nuget.Search(NuGetServiceIndex.PluginSearchTag, cancelSource.Token))
                    {
                        NuGetSearchResultVersion latestVersion = result.versions.FirstOrDefault(v => v.version == result.version);
                        NuGetPluginVM pluginVM = await this.TryCreatePluginVM(result, latestVersion, nuget, cancelSource.Token);
                        if (pluginVM != null)
                        {
                            finalPlugins.Add(pluginVM);
                        }
                    }
#if DEBUG
                    await Task.Delay(2000, cancelSource.Token);
#endif
                }
            }
            catch (Exception ex)
            {
                this.InfoBar.SetError(ex);
            }

            // Maybe a NuGet package was somehow removed from the server, so remove it from the cache too
            foreach (NuGetPluginVM removedPluginVM in initialPlugins.Except(finalPlugins).ToArray())
            {
                if (removedPluginVM.IsInstalled)
                {
                    // Keep it around, but don't allow updates
                    removedPluginVM.PluginSettings.LatestVersion = string.Empty;
                    removedPluginVM.PluginSettings.LatestVersionUrl = string.Empty;
                }
                else
                {
                    bool s1 = this.AppSettings.NuGetPlugins.Remove(removedPluginVM.PluginSettings);
                    bool s2 = this.plugins.Remove(removedPluginVM);
                    Debug.Assert(s1 && s2);
                }
            }
        }

        private bool FilterPlugin(NuGetSearchResult result, NuGetSearchResultVersion latestVersion)
        {
            if (!string.Equals(result.type, "Package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrEmpty(latestVersion.version) || string.IsNullOrEmpty(latestVersion.idUrl))
            {
                return false;
            }

            if (result.tags == null)
            {
                return false;
            }

            if (!result.tags.Contains(NuGetServiceIndex.PluginSearchTag, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (result.tags.Contains(NuGetServiceIndex.PluginSearchHiddenTag, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private async Task<NuGetPluginVM> TryCreatePluginVM(NuGetSearchResult plugin, NuGetSearchResultVersion latestVersion, NuGetServiceIndex nuget, CancellationToken cancelToken)
        {
            if (!this.FilterPlugin(plugin, latestVersion))
            {
                return null;
            }

            NuGetPluginVM pluginVM = this.plugins.OfType<NuGetPluginVM>().FirstOrDefault(p => p.PluginSettings.Id == plugin.id);
            NuGetPluginSettings pluginSettings = pluginVM?.PluginSettings ?? new NuGetPluginSettings();

            // Only need to fetch the version info once, then it's cached until there is a new package version available
            bool needsVersionInfo = pluginVM == null ||
                pluginSettings.LatestVersionUrl != latestVersion.idUrl ||
                string.IsNullOrEmpty(pluginSettings.LatestVersionPackageUrl) ||
                pluginSettings.LatestVersionDate == DateTime.MinValue;

            pluginSettings.Id = plugin.id;
            pluginSettings.Title = plugin.title;
            pluginSettings.Description = plugin.description;
            pluginSettings.Summary = plugin.summary;
            pluginSettings.ProjectUrl = plugin.projectUrl;
            pluginSettings.IconUrl = plugin.iconUrl;
            pluginSettings.Authors = string.Join(", ", plugin.authors);
            pluginSettings.LatestVersion = latestVersion.version;
            pluginSettings.LatestVersionUrl = latestVersion.idUrl;

            if (needsVersionInfo)
            {
                try
                {
                    NuGetPackageVersionInfo versionInfo = await nuget.GetVersionInfo(latestVersion, cancelToken);
                    pluginSettings.LatestVersionPackageUrl = versionInfo.packageContent;
                    pluginSettings.LatestVersionDate = versionInfo.published;
                }
                catch
                {
                    // Can't fetch the version info, so just don't show in the UI.
                    // Not sure if showing a plugin in an error state is useful to the user.
                    return null;
                }
            }

            if (pluginVM == null)
            {
                pluginVM = new NuGetPluginVM(this.App, this.AppSettings, pluginSettings);
                this.plugins.Add(pluginVM);
                this.AppSettings.NuGetPlugins.Add(pluginSettings);
            }

            return pluginVM;
        }

        public bool IsBusy
        {
            get => this.isBusy;
            set => this.SetPropertyValue(ref this.isBusy, value);
        }

        public PluginSortVM Sort
        {
            get => this.sort;
            set
            {
                if (this.SetPropertyValue(ref this.sort, value) && CollectionViewSource.GetDefaultView(this.plugins) is ListCollectionView view)
                {
                    view.CustomSort = this.sort.Comparer;
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
                try
                {
                    using (this.BeginBusy(out CancellationTokenSource cancelSource))
                    using (this.ProgressBar.Begin(cancelSource.Cancel, string.Format(CultureInfo.CurrentCulture, Resources.Plugins_InstallingProgress, plugin.Title)))
                    {
                        await plugin.Install(cancelSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    this.InfoBar.SetError(ex);
                }
            }
        });

        public ICommand UninstallPluginCommand => new DelegateCommand(async (object obj) =>
        {
            if (obj is IPluginVM plugin)
            {
                try
                {
                    using (this.BeginBusy(out CancellationTokenSource cancelSource))
                    using (this.ProgressBar.Begin(cancelSource.Cancel, string.Format(CultureInfo.CurrentCulture, Resources.Plugins_UninstallingProgress, plugin.Title)))
                    {
                        await plugin.Uninstall(cancelSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    this.InfoBar.SetError(ex);
                }
            }
        });

        private IDisposable BeginBusy(out CancellationTokenSource cancelSource)
        {
            this.IsBusy = true;
            IDisposable dialogBusy = this.settingsVM.BeginBusy();

            CancellationTokenSource myCancelSource = new CancellationTokenSource();
            cancelSource = myCancelSource;

            void onUnloaded(object s, RoutedEventArgs a) => myCancelSource.Cancel();
            this.settingsVM.Dialog.Unloaded += onUnloaded;

            return new DelegateDisposable(() =>
            {
                this.settingsVM.Dialog.Unloaded -= onUnloaded;
                myCancelSource.Dispose();
                dialogBusy.Dispose();
                this.IsBusy = false;
            });
        }
    }
}
