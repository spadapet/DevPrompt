using DevPrompt.ProcessWorkspace.Utility;

namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal class MainWindowDesignerVM : PropertyNotifier
    {
        public MainWindowDesignerVM()
        {
        }

        public string WindowTitle => "Window Title";
    }
}
