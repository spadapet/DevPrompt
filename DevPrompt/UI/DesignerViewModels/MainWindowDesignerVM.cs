namespace DevPrompt.UI.DesignerViewModels
{
    /// <summary>
    /// Sample data for the XAML designer
    /// </summary>
    internal sealed class MainWindowDesignerVM : Api.Utility.PropertyNotifier
    {
        public MainWindowDesignerVM()
        {
        }

        public string WindowTitle => "Window Title";
    }
}
