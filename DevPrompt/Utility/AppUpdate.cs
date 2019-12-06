using DevPrompt.UI;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DevPrompt.Utility
{
    /// <summary>
    /// Checks for updates and allows an update file to be downloaded.
    /// The update check happens:
    /// - A short period of time after startup
    /// - At least once a day afterwards
    /// - When the user chooses a menu item to force a check
    /// </summary>
    internal sealed class AppUpdate : Api.Utility.PropertyNotifier, Api.IAppUpdate, IDisposable
    {
        private readonly App app;
        private readonly Random random;
        private DispatcherTimer timer;
        private Api.AppUpdateState state;
        private string updateVersionString;
        private int lastUpdateTicks;

        private static TimeSpan InitialInterval = TimeSpan.FromSeconds(10);
        private static TimeSpan RestartInterval = TimeSpan.FromSeconds(1);
        private const int MinIntervalSeconds = 16 * 60 * 60;
        private const int MaxIntervalSeconds = 24 * 60 * 60;
        private const int MinTicksBetweenUpdates = 60 * 1000;

        public AppUpdate(App app)
        {
            this.app = app;
            this.random = new Random();
        }

        public void Dispose()
        {
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer = null;
            }
        }

        public void Start()
        {
            if (this.timer == null)
            {
                this.timer = new DispatcherTimer(AppUpdate.InitialInterval, DispatcherPriority.ApplicationIdle, this.OnTimer, this.app.Dispatcher);
            }
            else
            {
                this.timer.Interval = AppUpdate.RestartInterval;
            }
        }

        public Api.AppUpdateState State
        {
            get => this.state;
            set => this.SetPropertyValue(ref this.state, value);
        }

        public string UpdateVersionString
        {
            get => this.updateVersionString ?? this.CurrentVersionString;
            set => this.SetPropertyValue(ref this.updateVersionString, value);
        }

        public string CurrentVersionString => Program.VersionString;

        public async Task CheckUpdateVersionAsync()
        {
            int ticks = Environment.TickCount;
            if (this.lastUpdateTicks != 0 && ticks >= this.lastUpdateTicks && ticks - this.lastUpdateTicks < AppUpdate.MinTicksBetweenUpdates)
            {
                // Just recently checked
                return;
            }

            this.lastUpdateTicks = ticks;

            // Automatically check again up to a day later (random)
            int intervalSeconds = this.random.Next(AppUpdate.MinIntervalSeconds, AppUpdate.MaxIntervalSeconds);
            this.timer.Interval = TimeSpan.FromSeconds(intervalSeconds);

            string versionString = null;

            if (this.app is Api.IApp app && app.ActiveWindow is Api.IWindow window)
            {
                using (HttpClient client = new HttpClient())
                using (window.ProgressBar.Begin(client.CancelPendingRequests, Resources.AppUpdate_CheckProgress))
                {
                    try
                    {
                        versionString = await client.GetStringAsync(Resources.AppUpdate_LatestVersionLink);
                    }
                    catch
                    {
                        versionString = null;
                    }
                }
            }

            if (!string.IsNullOrEmpty(versionString) && Version.TryParse(versionString, out Version updateVersion))
            {
                this.UpdateVersionString = versionString;
                this.State = (this.State == Api.AppUpdateState.HasUpdate || Program.Version.CompareTo(updateVersion) < 0)
                    ? Api.AppUpdateState.HasUpdate
                    : Api.AppUpdateState.NoUpdate;
            }
        }

        public async Task DownloadUpdate(MainWindow mainWindow, string type)
        {
            Api.IWindow window = mainWindow.ViewModel;
            string url = string.Format(CultureInfo.CurrentCulture, Resources.AppUpdate_LatestFileDownload, type);

            string downloadFolder;
            try
            {
                downloadFolder = mainWindow.App.NativeApp.App.GetDownloadsFolder();
            }
            catch
            {
                downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            using (CancellationTokenSource cancelSource = new CancellationTokenSource())
            using (HttpClient client = new HttpClient())
            using (window.ProgressBar.Begin(() => cancelSource.Cancel(), Resources.AppUpdate_Downloading))
            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelSource.Token))
            {
                response.EnsureSuccessStatusCode();

                string suggestedFileName = response.Content?.Headers?.ContentDisposition?.FileName;
                if (string.IsNullOrEmpty(suggestedFileName))
                {
                    suggestedFileName = $"DevPrompt.{type}";
                }

                Task<string> downloadTask = Task.Run(() => FileUtility.DownloadFileAsync(response.Content, cancelSource.Token), cancelSource.Token);

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = Resources.AppUpdate_DownloadDialogTitle,
                    Filter = $"{string.Format(CultureInfo.InvariantCulture, Resources.AppUpdate_DownloadFilter, type.ToUpperInvariant())}|*.{type.ToLowerInvariant()}",
                    DefaultExt = $".{type.ToLowerInvariant()}",
                    InitialDirectory = downloadFolder,
                    FileName = suggestedFileName,
                };

                if (dialog.ShowDialog(mainWindow) == true)
                {
                    string destFile = dialog.FileName;
                    string tempFile = await downloadTask;

                    if (!string.IsNullOrEmpty(tempFile) && FileUtility.TryMoveFile(tempFile, destFile))
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe")
                        {
                            Arguments = $@"/e, /select, ""{destFile}""",
                        });
                    }
                }
                else
                {
                    cancelSource.Cancel();
                    await downloadTask;
                }
            }
        }

        public async Task UpdateNow(MainWindow mainWindow)
        {
            // Needs to download DevPrompt.Update.exe, run it, and exit this process
            // (might not ever be implemented)
            await Task.CompletedTask;
        }

        private async void OnTimer(object sender, EventArgs args)
        {
            await this.CheckUpdateVersionAsync();
        }
    }
}
