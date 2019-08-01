using DevPrompt.Api;
using DevPrompt.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevPrompt.UI.ViewModels
{
    internal class NuGetPluginVM : PropertyNotifier, IPluginVM
    {
        public string Title => this.settings.Title;
        public string Description => this.settings.Description;
        public string Summary => this.settings.Summary;
        public string Authors => this.settings.Authors;

        private MainWindow window;
        private NuGetPluginSettings settings;
        private string latestVersion;
        private string latestVersionUrl;
        private string installedVersion;
        private bool installed;
        private bool installing;
        private BitmapImage icon;

        public NuGetPluginVM(MainWindow window, NuGetPluginSettings settings, string latestVersion, string latestVersionUrl)
        {
            this.window = window;
            this.settings = settings;
            this.latestVersion = latestVersion;
            this.latestVersionUrl = latestVersionUrl;
            this.installed = !string.IsNullOrEmpty(this.settings.Path);
            this.installedVersion = this.installed ? this.settings.Version : string.Empty;
        }

        public Uri ProjectUrl
        {
            get
            {
                return !string.IsNullOrEmpty(this.settings.ProjectUrl) &&
                    Uri.TryCreate(this.settings.ProjectUrl, UriKind.Absolute, out Uri uri)
                    ? uri : null;
            }
        }

        public ImageSource Icon
        {
            get
            {
                if (this.icon == null)
                {
                    if (!string.IsNullOrEmpty(this.settings.IconUrl) && Uri.TryCreate(this.settings.IconUrl, UriKind.Absolute, out Uri uri))
                    {
                        this.icon = new BitmapImage(uri);
                    }
                    else
                    {
                        this.icon = new BitmapImage(new Uri("pack://application:,,,/UI/Images/default-package-icon.png"));
                    }
                }

                return this.icon;
            }
        }

        public string LatestVersion
        {
            get => this.latestVersion;
            set => this.SetPropertyValue(ref this.latestVersion, value ?? string.Empty);
        }

        public string LatestVersionUrl
        {
            get => this.latestVersionUrl;
            set => this.SetPropertyValue(ref this.latestVersionUrl, value ?? string.Empty);
        }

        public bool IsInstalling
        {
            get => this.installing;
            set => this.SetPropertyValue(ref this.installing, value);
        }

        public bool IsInstalled
        {
            get => this.installed;
            set => this.SetPropertyValue(ref this.installed, value);
        }

        public string InstalledVersion
        {
            get => this.installedVersion;
            set => this.SetPropertyValue(ref this.installedVersion, value ?? string.Empty);
        }

        public async Task Install(CancellationToken cancelToken)
        {
            if (!this.IsInstalled && !this.IsInstalling && this.InstalledVersion != this.LatestVersion)
            {
                this.IsInstalling = true;
                Exception exception = null;

                try
                {
                    Api.IHttpClient http = this.window.App.HttpClient;
                    Api.IJsonValue value = await http.GetJsonAsync(this.LatestVersionUrl, cancelToken);
                    string contentUrl = value["packageContent"].String;

                    HttpResponseMessage response = await http.Client.GetAsync(contentUrl, HttpCompletionOption.ResponseHeadersRead, cancelToken);
                    response = response.EnsureSuccessStatusCode();

                    using (Stream zipStream = await response.Content.ReadAsStreamAsync())
                    using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
                    {
                        await this.Unzip(zip, cancelToken);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    this.IsInstalling = false;
                    this.installedVersion = this.LatestVersion;
                    this.IsInstalled = true;
                }
                else
                {
                    this.window.ViewModel.SetError(exception);
                }
            }
        }

        public async Task Uninstall(CancellationToken cancelToken)
        {
            if (this.IsInstalled && !this.IsInstalling)
            {
                this.IsInstalling = true;
                Exception exception = null;

                try
                {
                    await Task.Run(() =>
                    {
                        string rootDir = Path.Combine(AppSettings.RootNuGetPluginDirectory, this.settings.Id);
                        Directory.Delete(rootDir, recursive: true);
                    });
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    this.IsInstalling = false;
                    this.InstalledVersion = string.Empty;
                    this.IsInstalled = false;
                }
                else
                {
                    this.window.ViewModel.SetError(exception);
                }
            }
        }

        private async Task Unzip(ZipArchive zip, CancellationToken cancelToken)
        {
            string rootDir = Path.Combine(AppSettings.RootNuGetPluginDirectory, this.settings.Id, this.LatestVersion);
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, recursive: true);
            }

            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                const string prefix = "tools/net40/";
                if (entry.FullName.StartsWith(prefix))
                {
                    string path = Path.Combine(rootDir, entry.FullName.Substring(prefix.Length));
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
