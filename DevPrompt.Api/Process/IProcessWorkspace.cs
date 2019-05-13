namespace DevPrompt.Api
{
    public interface IProcessWorkspace : ITabWorkspace
    {
        IProcessHost ProcessHost { get; }

        ITabVM FindTab(IProcess process);
        ITabVM RunProcess(IConsoleSettings settings);
        ITabVM RunProcess(string executable, string arguments, string startingDirectory, string tabName);
        ITabVM RestoreProcess(string state, string tabName);
        ITabVM CloneProcess(ITab tab, string tabName);
    }
}
