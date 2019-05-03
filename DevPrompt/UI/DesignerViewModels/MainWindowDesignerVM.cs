using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Generic;
using System.Windows.Input;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class MainWindowDesignerVM : PropertyNotifier
    {
        private List<ITabVM> tabs;

        public MainWindowDesignerVM()
        {
            this.tabs = new List<ITabVM>()
            {
                new TabDesignerVM()
                {
                    Active = true,
                },
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
        public ICommand DetachAndExitCommand => null;
        public ICommand VisualStudioInstallerCommand => null;
        public ICommand VisualStudioDogfoodCommand => null;
        public ICommand InstallVisualStudioBranchCommand => null;
        public ICommand CustomizeConsolesCommand => null;
        public ICommand SettingsImportCommand => null;
        public ICommand SettingsExportCommand => null;
        public ICommand CustomizeConsoleGrabCommand => null;
        public ICommand CustomizeToolsCommand => null;
        public ICommand CustomizeLinksCommand => null;
        public ICommand AboutCommand => null;

        public IReadOnlyList<ITabVM> Tabs => this.tabs;
        public ITabVM ActiveTab => this.tabs[0];
        public bool HasActiveTab => this.ActiveTab != null;
        public bool Loading => true;
        public bool NotLoading => !this.Loading;
        public bool HasErrorText => !string.IsNullOrEmpty(this.ErrorText);
        public string WindowTitle => "Window Title";
        public string ErrorText => "Error Text";
    }
}
