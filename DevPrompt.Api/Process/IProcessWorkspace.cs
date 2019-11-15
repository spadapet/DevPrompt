using System.Windows.Media;

namespace DevPrompt.Api
{
    public interface IProcessWorkspace : ITabWorkspace
    {
        IProcessHost ProcessHost { get; }

        ITabHolder FindTab(IProcess process);
        ITabHolder RunProcess(IConsoleSettings settings);
        ITabHolder RunProcess(string executable, string arguments, string startingDirectory, string tabName, Color themeKeyColor);
        ITabHolder RestoreProcess(string state, string tabName, Color themeKeyColor);
        ITabHolder CloneProcess(ITab tab, string tabName, Color themeKeyColor);
    }
}
