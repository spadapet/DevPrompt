using DevPrompt.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DevPrompt.Utility
{
    /// <summary>
    /// Helper for getting all the installed versions of Visual Studio
    /// </summary>
    internal sealed class VisualStudioSetup : Api.IVisualStudioSetup
    {
        public static async Task<IEnumerable<Api.IVisualStudioInstance>> GetInstancesAsync()
        {
            return await Task.Run(() =>
            {
                IVisualStudioInstances instances = NativeMethods.CreateVisualStudioInstances();

                int count = (instances != null) ? instances.GetCount() : 0;
                List<Api.IVisualStudioInstance> result = new List<Api.IVisualStudioInstance>(count);

                for (int i = 0; i < count; i++)
                {
                    result.Add(new VisualStudioInstance(instances.GetValue(i)));
                }

                return result;
            });
        }

        Task<IEnumerable<Api.IVisualStudioInstance>> Api.IVisualStudioSetup.GetInstancesAsync() => VisualStudioSetup.GetInstancesAsync();

        public string LocalInstallerPath
        {
            get
            {
                string programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles");
                string path = $@"{programFiles}\Microsoft Visual Studio\Installer\vs_installer.exe";
                return File.Exists(path) ? path : this.OnlineInstallerUrl;
            }
        }

        public string OnlineInstallerUrl => Program.IsMicrosoftDomain
            ? "http://aka.ms/vs/dogfood/install"
            : "http://aka.ms/vs";
    }
}
