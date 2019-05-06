using DevPrompt.UI.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace DevPrompt.UI
{
    internal partial class AboutDialog : Window
    {
        private IMainWindowVM window;

        public AboutDialog(IMainWindowVM window)
        {
            this.window = window;
            this.InitializeComponent();
        }

        public string AppVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
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
                this.window.StartProcess(hyperlink.NavigateUri.ToString());
            }
        }
    }
}
