using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// A single "Tool" customizable by the user
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Name}")]
    internal class ToolSettings : Api.PropertyNotifier
    {
        private string name;
        private string command;
        private string arguments;

        public ToolSettings()
        {
            this.name = DevPrompt.Utility.CommandHelpers.SeparatorName;
            this.command = string.Empty;
            this.arguments = string.Empty;
        }

        public ToolSettings(ToolSettings copyFrom)
        {
            this.name = copyFrom.Name;
            this.command = copyFrom.Command;
            this.arguments = copyFrom.arguments;
        }

        public ToolSettings Clone()
        {
            return new ToolSettings(this);
        }

        [DataMember]
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.SetPropertyValue(ref this.name, value ?? string.Empty);
            }
        }

        [DataMember]
        public string Command
        {
            get
            {
                return this.command;
            }

            set
            {
                if (this.SetPropertyValue(ref this.command, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.ExpandedCommand));
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

        public string ExpandedCommand
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(this.Command);
            }
        }

        public string ExpandedArguments
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(this.Arguments);
            }
        }
    }
}
