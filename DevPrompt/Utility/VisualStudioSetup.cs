using DevPrompt.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DevPrompt.Utility
{
    /// <summary>
    /// Helper for getting all the installed versions of Visual Studio
    /// </summary>
    internal static class VisualStudioSetup
    {
        public class Instance
        {
            public string Name { get; }
            public string Id { get; }
            public string Path { get; }
            public string ProductPath { get; }
            public string Version { get; }
            public string Channel { get; }
            public string DisplayName { get; }

            public Instance(IVisualStudioInstance instance)
            {
                this.Name = instance.GetInstallationName();
                this.Id = instance.GetInstanceId();
                this.Path = instance.GetInstallationPath();
                this.ProductPath = System.IO.Path.Combine(this.Path, instance.GetProductPath());
                this.Version = instance.GetInstallationVersion();
                this.Channel = instance.GetChannelId();

                Match match = Regex.Match(this.Name, @"\A.+\+(?<Version>\d+\.\d+)\.(?<Branch>.+)\Z");
                if (!match.Success)
                {
                    match = Regex.Match(this.Name, @"\A.+/(?<Branch>.+)\+(?<Version>\d+\.\d+)\Z");
                }

                Debug.Assert(match.Success);
                if (match.Success)
                {
                    string dogfood = this.Channel.EndsWith(".IntPreview", StringComparison.Ordinal) ? "Dogfood " : string.Empty;
                    this.DisplayName = $"{dogfood}{match.Groups["Branch"].Value} ({match.Groups["Version"].Value})";
                }
                else
                {
                    this.DisplayName = this.Name;
                }
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is Instance other && this.Id == other.Id && this.Version == other.Version;
            }
        }

        public static async Task<IEnumerable<Instance>> GetInstances()
        {
            return await Task.Run(() =>
            {
                IVisualStudioInstances instances = Interop.App.CreateVisualStudioInstances();

                int count = (instances != null) ? instances.GetCount() : 0;
                List<Instance> result = new List<Instance>(count);

                for (int i = 0; i < count; i++)
                {
                    result.Add(new Instance(instances.GetValue(i)));
                }

                return result;
            });
        }

        public static string InstallerPath
        {
            get
            {
                string path = @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vs_installer.exe";
                string expandedPath = Environment.ExpandEnvironmentVariables(path);
                return File.Exists(expandedPath) ? expandedPath : VisualStudioSetup.DogfoodInstallerPath;
            }
        }

        public static string DogfoodInstallerPath
        {
            get
            {
                return Program.IsMicrosoftDomain
                    ? "http://aka.ms/vs/dogfood/install"
                    : "http://aka.ms/vs";
            }
        }
    }
}
