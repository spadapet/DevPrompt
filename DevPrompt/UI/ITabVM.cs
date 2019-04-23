using System.Windows.Input;

namespace DevPrompt.UI
{
    public interface ITabVM
    {
        void Focus();

        string TabName { get; set; }
        string ExpandedTabName { get; }
        string Title { get; set; }
        bool Active { get; }
        bool InternalActive { get; set; }
        bool UsesProcessHost { get; }

        ICommand ActivateCommand { get; }
        ICommand CloneCommand { get; }
        ICommand CloseCommand { get; }
        ICommand DetachCommand { get; }
        ICommand DefaultsCommand { get; }
        ICommand PropertiesCommand { get; }
        ICommand SetTabNameCommand { get; }
    }
}
