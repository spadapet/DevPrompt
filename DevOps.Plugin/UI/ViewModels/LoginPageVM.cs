using DevPrompt.Api;

namespace DevOps.UI.ViewModels
{
    internal class LoginPageVM : PropertyNotifier
    {
        public IWindow Window => this.tab.Window;

        private readonly PullRequestTab tab;
        private string displayText;
        private string infoText;

        public LoginPageVM()
            : this(null)
        {
        }

        public LoginPageVM(PullRequestTab tab)
        {
            this.tab = tab;
            this.displayText = "Logging in...";
            this.infoText = string.Empty;
        }

        public string DisplayText
        {
            get => this.displayText;
            set => this.SetPropertyValue(ref this.displayText, value ?? string.Empty);
        }

        public string InfoText
        {
            get => this.infoText;
            set => this.SetPropertyValue(ref this.infoText, value ?? string.Empty);
        }
    }
}
