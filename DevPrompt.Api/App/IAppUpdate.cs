using System.ComponentModel;
using System.Threading.Tasks;

namespace DevPrompt.Api
{
    /// <summary>
    /// Lets you check if the app has an update online
    /// </summary>
    public interface IAppUpdate : INotifyPropertyChanged
    {
        AppUpdateState State { get; }
        string UpdateVersionString { get; }
        string CurrentVersionString { get; }

        Task CheckUpdateVersionAsync();
    }
}
