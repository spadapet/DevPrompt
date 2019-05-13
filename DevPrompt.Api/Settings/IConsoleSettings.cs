using System.ComponentModel;

namespace DevPrompt.Api
{
    public interface IConsoleSettings : INotifyPropertyChanged
    {
        string TabName { get; }
        string Executable { get; }
        string Arguments { get; }
        string StartingDirectory { get; }
        bool RunAtStartup { get; }
    }
}
