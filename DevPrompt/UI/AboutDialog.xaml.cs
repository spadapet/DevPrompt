using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace DevPrompt.UI
{
    internal partial class AboutDialog : Window
    {
        public string AppVersion => Program.Version.ToString();
        public Api.IAppUpdate AppUpdate => this.window.App.AppUpdate;
        private readonly Api.IWindow window;

        public AboutDialog(Api.IWindow window)
        {
            this.window = window;
            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            await this.AppUpdate.CheckUpdateVersionAsync();
        }

        public string CopyrightYear
        {
            get
            {
                AssemblyCopyrightAttribute copyright = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>();
                return copyright?.Copyright ?? string.Empty;
            }
        }

        private void OnHyperlink(object sender, RequestNavigateEventArgs args)
        {
            if (sender is Hyperlink hyperlink && hyperlink.NavigateUri != null)
            {
                this.window.App.RunExternalProcess(hyperlink.NavigateUri.ToString());
            }
        }
    }
}
