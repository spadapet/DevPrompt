using DevPrompt.Interop;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DevPrompt.Utility
{
    internal sealed class VisualStudioInstance : Api.IVisualStudioInstance
    {
        public string Name { get; }
        public string Id { get; }
        public string Path { get; }
        public string ProductPath { get; }
        public string Version { get; }
        public string Channel { get; }
        public string DisplayName { get; }

        public VisualStudioInstance(IVisualStudioInstance instance)
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
            return obj is VisualStudioInstance other && this.Id == other.Id && this.Version == other.Version;
        }
    }
}
