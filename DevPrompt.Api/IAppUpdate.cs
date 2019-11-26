using System.ComponentModel;
using System.Threading.Tasks;

namespace DevPrompt.Api
{
    public interface IAppUpdate : INotifyPropertyChanged
    {
        AppUpdateState State { get; }
        string UpdateVersionString { get; }

        Task CheckUpdateVersionAsync();
    }

    public enum AppUpdateState
    {
        Unknown,
        NoUpdate,
        HasUpdate,
    }
}
