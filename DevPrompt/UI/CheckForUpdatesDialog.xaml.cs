using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace DevPrompt.UI
{
    internal partial class CheckForUpdatesDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private string latestVersion;
        private Api.IWindow window;
        private HttpClient httpClient;

        public CheckForUpdatesDialog(Api.IWindow window)
        {
            this.window = window;
            this.latestVersion = "checking...";
            this.InitializeComponent();
        }

        public string LatestVersion
        {
            get => this.latestVersion;

            set
            {
                this.latestVersion = value ?? string.Empty;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LatestVersion)));
            }
        }

        private void OnHyperlink(object sender, RequestNavigateEventArgs args)
        {
            if (sender is Hyperlink hyperlink && hyperlink.NavigateUri != null)
            {
                this.window.RunExternalProcess(hyperlink.NavigateUri.ToString());
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            using (HttpClient client = new HttpClient())
            using (this.window.BeginLoading(client.CancelPendingRequests, "Checking latest version"))
            {
                try
                {
                    this.httpClient = client;
                    string versionString = await client.GetStringAsync(@"http://peterspada.com/DevPrompt/GetLatestVersion");
                    this.LatestVersion = versionString;
                }
                catch
                {
                    // doesn't matter
                    this.LatestVersion = "failed";
                }
                finally
                {
                    this.httpClient = null;
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (this.httpClient != null)
            {
                this.httpClient.CancelPendingRequests();
            }
        }
    }
}
