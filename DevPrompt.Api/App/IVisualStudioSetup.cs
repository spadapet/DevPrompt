using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevPrompt.Api
{
    /// <summary>
    /// Lets you get information about the installed versions of Visual Studio
    /// </summary>
    public interface IVisualStudioSetup
    {
        Task<IEnumerable<IVisualStudioInstance>> GetInstancesAsync();
        string LocalInstallerPath { get; }
        string OnlineInstallerUrl { get; }
    }
}
