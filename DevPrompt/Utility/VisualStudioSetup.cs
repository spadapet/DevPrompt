using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Setup.Configuration;

namespace DevPrompt.Utility
{
    /// <summary>
    /// Helper for getting all the installed versions of Visual Studio
    /// </summary>
    internal static class VisualStudioSetup
    {
        public class Instance
        {
            public string DisplayName { get; }
            public string Name { get; }
            public string Id { get; }
            public string Path { get; }
            public string ProductPath { get; }
            public string Version { get; }
            public string Channel { get; }

            public Instance(ISetupInstance2 instance)
            {
                this.Name = instance.GetInstallationName();
                this.Id = instance.GetInstanceId();
                this.Path = instance.GetInstallationPath();
                this.ProductPath = System.IO.Path.Combine(this.Path, instance.GetProductPath());
                this.Version = instance.GetInstallationVersion();

                ISetupPropertyStore props = instance as ISetupPropertyStore;
                this.Channel = props?.GetValue("channelId") as string ?? string.Empty;

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

        public static Task<IEnumerable<Instance>> GetInstancesAsync()
        {
            return Task.Run(() => VisualStudioSetup.GetInstances());
        }

        public static IEnumerable<Instance> GetInstances()
        {
            List<Instance> instances = new List<Instance>();

            try
            {
                SetupConfiguration setup = new SetupConfiguration();
                IEnumSetupInstances instanceEnumerator = setup.EnumInstances();
                ISetupInstance[] enumInstances = new ISetupInstance[1];
                int fetched = 0;

                do
                {
                    instanceEnumerator.Next(1, enumInstances, out fetched);
                    if (fetched == 1 && enumInstances[0] is ISetupInstance2 instance && instance.IsLaunchable())
                    {
                        instances.Add(new Instance(instance));
                    }
                }
                while (fetched == 1);
            }
            catch
            {
                // VS installer may not be installed or fail in some way
            }

            return instances;
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
