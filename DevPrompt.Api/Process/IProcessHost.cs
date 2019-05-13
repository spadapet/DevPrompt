using System;

namespace DevPrompt.Api
{
    /// <summary>
    /// All processes are owned by a process host. The main window gives access to a process host.
    /// </summary>
    public interface IProcessHost : IDisposable
    {
        IntPtr Hwnd { get; }

        void Focus();
        void Activate();
        void Deactivate();
        void Show();
        void Hide();

        IProcess RunProcess(string executable, string arguments, string startingDirectory);
        IProcess RestoreProcess(string state);
        IProcess CloneProcess(IProcess process);
        bool ContainsProcess(IProcess process);
    }
}
