using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Threading;
using DevPrompt.Api;
using DevPrompt.ProcessWorkspace.Utility;

namespace DevPrompt.Utility
{
    internal class AppUpdate : PropertyNotifier, Api.IAppUpdate, IDisposable
    {
        private readonly App app;
        private readonly Random random;
        private DispatcherTimer timer;
        private AppUpdateState state;
        private string updateVersionString;
        private bool checkingForUpdate;

        private static TimeSpan InitialInterval = TimeSpan.FromSeconds(10);
        private static TimeSpan RestartInterval = TimeSpan.FromSeconds(1);
        private const int MinIntervalSeconds = 16 * 60 * 60;
        private const int MaxIntervalSeconds = 24 * 60 * 60;

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

        public AppUpdateState State
        {
            get => this.state;
            set => this.SetPropertyValue(ref this.state, value);
        }

        public string UpdateVersionString
        {
            get => this.updateVersionString ?? Program.Version.ToString();
            set => this.SetPropertyValue(ref this.updateVersionString, value);
        }

        public async Task CheckUpdateVersionAsync()
        {
            // Automatically check again up to a day later (random)
            int intervalSeconds = this.random.Next(AppUpdate.MinIntervalSeconds, AppUpdate.MaxIntervalSeconds);
            this.timer.Interval = TimeSpan.FromSeconds(intervalSeconds);

            string versionString = null;

            if (!this.checkingForUpdate && this.app is Api.IApp app && app.ActiveWindow is Api.IWindow window)
            {
                using (HttpClient client = new HttpClient())
                using (window.ProgressBar.Begin(client.CancelPendingRequests, Resources.AppUpdate_CheckProgress))
                {
                    try
                    {
                        this.checkingForUpdate = true;
                        versionString = await client.GetStringAsync(Resources.AppUpdate_LatestVersionLink);
                    }
                    catch
                    {
                        versionString = null;
                    }
                    finally
                    {
                        this.checkingForUpdate = false;
                    }
                }
            }

            if (versionString != null && Version.TryParse(versionString, out Version updateVersion))
            {
                this.UpdateVersionString = versionString;
                this.State = (Program.Version.CompareTo(updateVersion) > 0) ? AppUpdateState.HasUpdate : AppUpdateState.NoUpdate;
            }
        }

        private async void OnTimer(object sender, EventArgs args)
        {
            await this.CheckUpdateVersionAsync();
        }
    }
}
