using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevPrompt.Api
{
    public interface IVisualStudioSetup
    {
        Task<IEnumerable<IVisualStudioInstance>> GetInstancesAsync();
        string LocalInstallerPath { get; }
        string OnlineInstallerUrl { get; }
    }
}
