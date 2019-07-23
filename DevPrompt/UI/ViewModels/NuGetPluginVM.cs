using DevPrompt.Api;
using DevPrompt.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevPrompt.UI.ViewModels
{
    internal class NuGetPluginVM : PropertyNotifier, IPluginVM
    {
        public string Title => this.settings.Title;
        public string Description => this.settings.Description;
        public string Summary => this.settings.Summary;
        public string Version => this.version;
        public string VersionUrl => this.versionUrl;
        public string Authors => this.settings.Authors;

        private NuGetPluginSettings settings;
        private string version;
        private string versionUrl;
        private string installedVersion;
        private bool installed;
        private bool installing;
        private BitmapImage icon;

        public NuGetPluginVM(NuGetPluginSettings settings, string latestVersion, string latestVersionUrl)
        {
            this.settings = settings;
            this.version = latestVersion;
            this.versionUrl = latestVersionUrl;
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
            get => this.version;
            set => this.SetPropertyValue(ref this.version, value ?? string.Empty);
        }

        public string LatestVersionUrl
        {
            get => this.versionUrl;
            set => this.SetPropertyValue(ref this.versionUrl, value ?? string.Empty);
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
            if (!this.IsInstalled && !this.IsInstalling && this.InstalledVersion != this.Version)
            {
                this.IsInstalling = true;

                await Task.Delay(2000, cancelToken);

                this.IsInstalling = false;
                this.installedVersion = this.Version;
                this.IsInstalled = true;
            }
        }

        public async Task Uninstall(CancellationToken cancelToken)
        {
            if (this.IsInstalled && !this.IsInstalling)
            {
                this.IsInstalling = true;

                await Task.Delay(1000, cancelToken);

                this.IsInstalling = false;
                this.InstalledVersion = string.Empty;
                this.IsInstalled = false;
            }
        }
    }
}
