using DevPrompt.Settings;
using System.Collections.Generic;
using System.Windows.Input;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class MainWindowDesignerVM : Api.PropertyNotifier
    {
        private List<Api.ITab> tabs;

        public MainWindowDesignerVM()
        {
            this.tabs = new List<Api.ITab>()
            {
                new TabDesignerVM(Api.ActiveState.Active),
                new TabDesignerVM(),
                new TabDesignerVM(),
                new TabDesignerVM(),
            };
        }

        public ICommand ConsoleCommand => null;
        public ICommand GrabConsoleCommand => null;
        public ICommand LinkCommand => null;
        public ICommand ToolCommand => null;
        public ICommand VisualStudioCommand => null;
        public ICommand ClearErrorTextCommand => null;
        public ICommand ExitCommand => null;
        public ICommand VisualStudioInstallerCommand => null;
        public ICommand VisualStudioDogfoodCommand => null;
        public ICommand InstallVisualStudioBranchCommand => null;
        public ICommand SettingsCommand => null;
        public ICommand CustomizeConsoleGrabCommand => null;
        public ICommand CustomizeToolsCommand => null;
        public ICommand CustomizeLinksCommand => null;
        // Help menu
        public ICommand ReportAnIssueCommand => null;
        public ICommand CheckForUpdatesCommand => null;
        public ICommand AboutCommand => null;

        public AppSettings AppSettings => null;
        public IReadOnlyList<Api.ITab> Tabs => this.tabs;
        public Api.ITab ActiveTab => this.tabs[0];
        public bool HasActiveTab => this.ActiveTab != null;
        public bool Loading => true;
        public bool HasErrorText => !string.IsNullOrEmpty(this.ErrorText);
        public string WindowTitle => "Window Title";
        public string ErrorText => "Error Text";
    }
}
