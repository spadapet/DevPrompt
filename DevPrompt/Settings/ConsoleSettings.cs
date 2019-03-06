using DevPrompt.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    [DataContract]
    [DebuggerDisplay("{MenuName}")]
    public class ConsoleSettings : PropertyNotifier, ICloneable
    {
        private string menuName;
        private string tabName;
        private string startingDirectory;
        private string arguments;
        private bool runAtStartup;
        private ConsoleType consoleType;

        public ConsoleSettings()
        {
            this.menuName = @"Command Prompt";
            this.tabName = @"Cmd";
            this.arguments = string.Empty;
            this.startingDirectory = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).Select(d => d.Name).FirstOrDefault();
            this.consoleType = ConsoleType.Cmd;
        }

        public ConsoleSettings(ConsoleSettings copyFrom)
        {
            this.menuName = copyFrom.MenuName;
            this.tabName = copyFrom.TabName;
            this.startingDirectory = copyFrom.StartingDirectory;
            this.arguments = copyFrom.arguments;
            this.consoleType = copyFrom.ConsoleType;
            this.runAtStartup = copyFrom.RunAtStartup;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public ConsoleSettings Clone()
        {
            return new ConsoleSettings(this);
        }

        [DataMember]
        public string MenuName
        {
            get
            {
                return this.menuName;
            }

            set
            {
                this.SetPropertyValue(ref this.menuName, value ?? string.Empty);
            }
        }

        [DataMember]
        public string TabName
        {
            get
            {
                return this.tabName;
            }

            set
            {
                this.SetPropertyValue(ref this.tabName, value ?? string.Empty);
            }
        }

        [DataMember]
        public string StartingDirectory
        {
            get
            {
                return this.startingDirectory;
            }

            set
            {
                if (this.SetPropertyValue(ref this.startingDirectory, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.ExpandedStartingDirectory));
                }
            }
        }

        [DataMember]
        public string Arguments
        {
            get
            {
                return this.arguments;
            }

            set
            {
                if (this.SetPropertyValue(ref this.arguments, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.ExpandedArguments));
                }
            }
        }

        [DataMember]
        public ConsoleType ConsoleType
        {
            get
            {
                return this.consoleType;
            }

            set
            {
                if (this.SetPropertyValue(ref this.consoleType, value))
                {
                    this.OnPropertyChanged(nameof(this.Executable));
                }
            }
        }

        [DataMember]
        public bool RunAtStartup
        {
            get
            {
                return this.runAtStartup;
            }

            set
            {
                this.SetPropertyValue(ref this.runAtStartup, value);
            }
        }

        public string Executable
        {
            get
            {
                return ConsoleSettings.GetExecutable(this.ConsoleType);
            }
        }

        public static string GetExecutable(ConsoleType type)
        {
            switch (type)
            {
                case ConsoleType.Cmd:
                    return Environment.ExpandEnvironmentVariables(@"%windir%\System32\cmd.exe");

                case ConsoleType.PowerShell:
                    return Environment.ExpandEnvironmentVariables(@"%windir%\System32\WindowsPowerShell\v1.0\powershell.exe");

                default:
                    Debug.Fail($"Unknown console type: {Enum.GetName(typeof(ConsoleType), type)}");
                    return string.Empty;
            }
        }

        public string ExpandedArguments
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(this.Arguments);
            }
        }

        public string ExpandedStartingDirectory
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(this.StartingDirectory);
            }
        }
    }
}
