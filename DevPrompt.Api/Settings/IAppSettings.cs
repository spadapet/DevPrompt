using System.Collections.Generic;
using System.ComponentModel;

namespace DevPrompt.Api
{
    /// <summary>
    /// Import this to be able to get/set custom settings that are persisted.
    /// You must Export an ISettingTypes in order to able to load setting types
    /// that come from your plugin.
    /// </summary>
    public interface IAppSettings : INotifyPropertyChanged
    {
        IEnumerable<IConsoleSettings> ConsoleSettings { get; }

        bool ConsoleGrabEnabled { get; set; }
        bool SaveTabsOnExit { get; set; }

        bool TryGetProperty<T>(string name, out T value);
        void SetProperty<T>(string name, T value);
        bool RemoveProperty(string name);
        string GetDefaultTabName(string path);
    }
}
