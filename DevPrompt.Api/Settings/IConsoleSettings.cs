namespace DevPrompt.Api
{
    public interface IConsoleSettings
    {
        string TabName { get; }
        string Executable { get; }
        string Arguments { get; }
        string StartingDirectory { get; }
        bool RunAtStartup { get; }
    }
}
