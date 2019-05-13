using System;
using System.ComponentModel;

namespace DevPrompt.Api
{
    /// <summary>
    /// A command prompt process hosted in a tab, provided by the app.
    /// Export an IProcessListener if you want to know when they are opened and closed.
    /// </summary>
    public interface IProcess : INotifyPropertyChanged, IDisposable
    {
        IntPtr Hwnd { get; }
        string State { get; }
        string Title { get; }
        string Environment { get; }

        void Focus();
        void Detach();
        void Activate();
        void Deactivate();

        void RunCommand(ProcessCommand command);
        string ExpandEnvironmentVariables(string text);
    }
}
