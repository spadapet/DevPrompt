using DevPrompt.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// User customizable info about a process that can be "Grabbed" by the UI
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{ExeName}")]
    internal class GrabConsoleSettings : PropertyNotifier
    {
        private string exeName;
        private string tabName;
        private bool tabActivate;

        public GrabConsoleSettings()
        {
            this.exeName = @"cmd.exe";
            this.tabName = @"cmd";
            this.tabActivate = true;
        }

        public GrabConsoleSettings(GrabConsoleSettings copyFrom)
        {
            this.exeName = copyFrom.exeName;
            this.tabName = copyFrom.tabName;
            this.tabActivate = copyFrom.tabActivate;
        }

        public GrabConsoleSettings Clone()
        {
            return new GrabConsoleSettings(this);
        }

        [DataMember]
        public string ExeName
        {
            get => this.exeName;

            set
            {
                if (this.SetPropertyValue(ref this.exeName, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.ExpandedExeName));
                }
            }
        }

        [DataMember]
        public string TabName
        {
            get => this.tabName;
            set => this.SetPropertyValue(ref this.tabName, value ?? string.Empty);
        }

        [DataMember]
        public bool TabActivate
        {
            get => this.tabActivate;
            set => this.SetPropertyValue(ref this.tabActivate, value);
        }

        public string ExpandedExeName => Environment.ExpandEnvironmentVariables(this.ExeName);

        public bool CanGrab(string exePath)
        {
            if (!string.IsNullOrEmpty(exePath))
            {
                string endsWith = this.ExpandedExeName;
                if (endsWith.Length > 0 && endsWith.Length < exePath.Length && endsWith[0] != Path.DirectorySeparatorChar)
                {
                    endsWith = endsWith.Insert(0, Path.DirectorySeparatorChar.ToString());
                }

                if (exePath.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
