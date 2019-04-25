using DevPrompt.Interop;

namespace DevPrompt.UI.ViewModels
{
    public interface IMainWindowVM
    {
        IProcessHost ProcessHost { get; }
        ITabVM FindTab(IProcess process);
    }
}
