using DevPrompt.Api;
using DevPrompt.Plugins;
using DevPrompt.Settings;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevPrompt.UI.ViewModels
{
    internal class NuGetPluginVM : PropertyNotifier, IPluginVM
    {
        public NuGetPluginSettings PluginSettings { get; }

        public string Title => this.PluginSettings.Title;
        public string Description => this.PluginSettings.Description;
        public string Summary => this.PluginSettings.Summary;
        public string InstalledVersion => this.PluginSettings.InstalledVersion;
        public string LatestVersion => this.PluginSettings.LatestVersion;
        public string Authors => this.PluginSettings.Authors;
        public bool IsInstalled => this.PluginSettings.IsInstalled;

        private App app;
        private AppSettings appSettings;
        private BitmapImage icon;
        private Uri projectUrl;
        private Task busyTask;

        public NuGetPluginVM(App app, AppSettings appSettings, NuGetPluginSettings pluginSettings)
        {
            this.app = app;
            this.appSettings = appSettings;
            this.PluginSettings = pluginSettings;
            this.PluginSettings.PropertyChanged += this.OnModelPropertyChanged;
        }

        public void Dispose()
        {
            this.PluginSettings.PropertyChanged -= this.OnModelPropertyChanged;
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            bool all = string.IsNullOrEmpty(args.PropertyName);

            if (all || args.PropertyName == nameof(this.PluginSettings.Title))
            {
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.Description))
            {
                this.OnPropertyChanged(nameof(this.Description));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.Summary))
            {
                this.OnPropertyChanged(nameof(this.Summary));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.ProjectUrl))
            {
                this.projectUrl = null;
                this.OnPropertyChanged(nameof(this.ProjectUrl));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.IconUrl))
            {
                this.icon = null;
                this.OnPropertyChanged(nameof(this.icon));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.Authors))
            {
                this.OnPropertyChanged(nameof(this.Authors));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.LatestVersion))
            {
                this.OnPropertyChanged(nameof(this.LatestVersion));
                this.OnPropertyChanged(nameof(this.State));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.InstalledVersion))
            {
                this.OnPropertyChanged(nameof(this.InstalledVersion));
                this.OnPropertyChanged(nameof(this.State));
            }

            if (all || args.PropertyName == nameof(this.PluginSettings.IsInstalled))
            {
                this.OnPropertyChanged(nameof(this.IsInstalled));
            }
        }

        public Uri ProjectUrl
        {
            get
            {
                if (this.projectUrl == null)
                {
                    if (!string.IsNullOrEmpty(this.PluginSettings.ProjectUrl) && Uri.TryCreate(this.PluginSettings.ProjectUrl, UriKind.Absolute, out Uri uri))
                    {
                        this.projectUrl = uri;
                    }
                }

                return this.projectUrl;
            }
        }

        public ImageSource Icon
        {
            get
            {
                if (this.icon == null)
                {
                    if (!string.IsNullOrEmpty(this.PluginSettings.IconUrl) && Uri.TryCreate(this.PluginSettings.IconUrl, UriKind.Absolute, out Uri uri))
                    {
                        this.icon = new BitmapImage(uri);
                    }
                }

                return this.icon;
            }
        }

        public PluginState State
        {
            get
            {
                PluginState state = PluginState.None;
                if (!string.IsNullOrEmpty(this.InstalledVersion))
                {
                    state |= PluginState.Installed;

                    if (!string.IsNullOrEmpty(this.LatestVersion) && this.LatestVersion != this.InstalledVersion)
                    {
                        state |= PluginState.UpdateAvailable;
                    }
                }

                if (this.busyTask != null)
                {
                    state |= PluginState.Busy;
                }

                return state;
            }
        }

        private Task SetBusyTask(Task task)
        {
            if (this.busyTask != task)
            {
                this.busyTask = task;
                this.OnPropertyChanged(nameof(this.State));

                if (task != null)
                {
                    this.app.AddCriticalTask(task);
                }
            }

            return task ?? Task.CompletedTask;
        }

        public async Task Install(CancellationToken cancelToken)
        {
            if (!this.State.HasFlag(PluginState.Busy) && (!this.State.HasFlag(PluginState.Installed) || this.State.HasFlag(PluginState.UpdateAvailable)))
            {
                try
                {
                    await this.SetBusyTask(this.InternalInstall(cancelToken));
                }
                finally
                {
                    await this.SetBusyTask(null);
                }
            }
        }

        public Task Uninstall(CancellationToken cancelToken)
        {
            if (!this.State.HasFlag(PluginState.Busy) && this.State.HasFlag(PluginState.Installed))
            {
                this.PluginSettings.InstalledInfo = null;
                this.appSettings.PluginsChanged = true;
            }

            return Task.CompletedTask;
        }

        private async Task InternalInstall(CancellationToken cancelToken)
        {
            string versionToInstall = this.LatestVersion;
            string versionToInstallPath = this.PluginSettings.GetInstallPath(versionToInstall);

            HttpResponseMessage contentResponse = await this.app.HttpClient.Client.GetAsync(this.PluginSettings.LatestVersionPackageUrl, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            contentResponse = contentResponse.EnsureSuccessStatusCode();

            using (Stream zipStream = await contentResponse.Content.ReadAsStreamAsync())
            using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                await this.Unzip(zip, versionToInstallPath, cancelToken);
            }

            InstalledPluginInfo installedInfo = new InstalledPluginInfo()
            {
                Id = this.PluginSettings.Id,
                Version = versionToInstall,
                RootPath = versionToInstallPath,
            };

            await installedInfo.GatherAssemblyInfo();

            this.PluginSettings.InstalledInfo = installedInfo;
            this.appSettings.PluginsChanged = true;
        }

        private async Task Unzip(ZipArchive zip, string rootDir, CancellationToken cancelToken)
        {
            string homeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                const string prefix = "tools/net40/";
                if (entry.FullName.StartsWith(prefix))
                {
                    string path = Path.Combine(rootDir, entry.FullName.Substring(prefix.Length));
                    if (File.Exists(path))
                    {
                        // Since NuGet package contents can't change, assume that if the file exists, it's good.
                        continue;
                    }

                    if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = Path.GetFileName(path);
                        if (fileName.Equals("DevPrompt.Api.dll", StringComparison.OrdinalIgnoreCase) ||
                            fileName.StartsWith("System.Composition.", StringComparison.OrdinalIgnoreCase))
                        {
                            // Don't install any DLLs that also exist in the install directory, I always want my own to be loaded.
                            continue;
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (Stream stream = entry.Open())
                    using (FileStream fileStream = File.Create(path))
                    {
                        const int defaultBufferSize = 81920;
                        await stream.CopyToAsync(fileStream, defaultBufferSize, cancelToken);
                    }
                }
            }
        }
    }
}
