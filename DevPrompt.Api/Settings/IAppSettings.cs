using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DevPrompt.Api
{
    /// <summary>
    /// Import this to be able to get/set custom settings that are persisted.
    /// </summary>
    public interface IAppSettings : INotifyPropertyChanged
    {
        IEnumerable<IConsoleSettings> ConsoleSettings { get; }
        Task<IEnumerable<IConsoleSettings>> GetVisualStudioConsoleSettingsAsync();
        IEnumerable<Color> TabThemeKeys { get; }
        ITabTheme GetTabTheme(Color keyColor);

        bool ConsoleGrabEnabled { get; set; }
        bool SaveTabsOnExit { get; set; }

        bool TryGetProperty<T>(string name, out T value);
        void SetProperty<T>(string name, T value);
        bool RemoveProperty(string name);
        string GetDefaultTabName(string path);
    }
}
