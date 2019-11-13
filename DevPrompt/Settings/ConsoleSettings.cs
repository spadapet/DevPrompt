﻿using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// User customizable info about a console process that can be run
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{MenuName}")]
    internal class ConsoleSettings : PropertyNotifier, IEquatable<ConsoleSettings>, Api.IConsoleSettings
    {
        string Api.IConsoleSettings.TabName => this.TabName;
        string Api.IConsoleSettings.Executable => this.Executable;
        string Api.IConsoleSettings.Arguments => this.ExpandedArguments;
        string Api.IConsoleSettings.StartingDirectory => this.ExpandedStartingDirectory;
        bool Api.IConsoleSettings.RunAtStartup => this.RunAtStartup;

        private string menuName;
        private string tabName;
        private string startingDirectory;
        private string arguments;
        private bool runAtStartup;
        private ConsoleType consoleType;

        public ConsoleSettings()
        {
            this.menuName = Resources.Menu_RawCommandName;
            this.tabName = Resources.Menu_RawCommandTabName;
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

        public ConsoleSettings Clone()
        {
            return new ConsoleSettings(this);
        }

        public override bool Equals(object obj)
        {
            return obj is ConsoleSettings other && this.Equals(other);
        }

        public bool Equals(ConsoleSettings other)
        {
            return this.menuName == other.menuName &&
                this.tabName == other.tabName &&
                this.startingDirectory == other.startingDirectory &&
                this.arguments == other.arguments &&
                this.runAtStartup == other.runAtStartup &&
                this.consoleType == other.consoleType;
        }

        public override int GetHashCode()
        {
            return HashUtility.CombineHashCodes(
                HashUtility.CombineHashCodes(this.menuName.GetHashCode(), this.tabName.GetHashCode()),
                HashUtility.CombineHashCodes(this.startingDirectory.GetHashCode(), this.arguments.GetHashCode()),
                HashUtility.CombineHashCodes(this.runAtStartup.GetHashCode(), this.consoleType.GetHashCode()));
        }

        [DataMember]
        public string MenuName
        {
            get => this.menuName;
            set => this.SetPropertyValue(ref this.menuName, value ?? string.Empty);
        }

        [DataMember]
        public string TabName
        {
            get => this.tabName;
            set => this.SetPropertyValue(ref this.tabName, value ?? string.Empty);
        }

        [DataMember]
        public string StartingDirectory
        {
            get => this.startingDirectory;

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
            get => this.arguments;

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
            get => this.consoleType;

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
            get => this.runAtStartup;
            set => this.SetPropertyValue(ref this.runAtStartup, value);
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

        public string Executable => ConsoleSettings.GetExecutable(this.ConsoleType);
        public string ExpandedArguments => Environment.ExpandEnvironmentVariables(this.Arguments);
        public string ExpandedStartingDirectory => Environment.ExpandEnvironmentVariables(this.StartingDirectory);
    }
}
