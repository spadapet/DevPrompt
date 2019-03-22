using DevPrompt.UI;
using DevPrompt.Utility;
using System;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves the state of a console during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    public class ConsoleSnapshot : PropertyNotifier, ICloneable
    {
        private string tabName;
        private string windowTitle;
        private string executable;
        private string currentDirectory;
        private string environment;
        private string aliases;
        private string colorTable;

        public ConsoleSnapshot()
            : this((ProcessVM)null)
        {
        }

        internal ConsoleSnapshot(ProcessVM process)
        {
            this.tabName = process?.TabName ?? string.Empty;
            this.windowTitle = process?.Title ?? string.Empty;
            this.executable = process?.Process?.GetExe() ?? string.Empty;
            this.currentDirectory = process?.Process?.GetCurrentDirectory() ?? string.Empty;
            this.environment = process?.Env ?? string.Empty;
            this.aliases = process?.Process?.GetAliases() ?? string.Empty;
            this.colorTable = process?.Process?.GetColorTable() ?? string.Empty;
        }

        public ConsoleSnapshot(ConsoleSnapshot copyFrom)
        {
            this.tabName = copyFrom.tabName;
            this.windowTitle = copyFrom.windowTitle;
            this.executable = copyFrom.executable;
            this.currentDirectory = copyFrom.currentDirectory;
            this.environment = copyFrom.environment;
            this.aliases = copyFrom.aliases;
            this.colorTable = copyFrom.colorTable;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public ConsoleSnapshot Clone()
        {
            return new ConsoleSnapshot(this);
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
        public string WindowTitle
        {
            get
            {
                return this.windowTitle;
            }

            set
            {
                this.SetPropertyValue(ref this.windowTitle, value ?? string.Empty);
            }
        }

        [DataMember]
        public string Executable
        {
            get
            {
                return this.executable;
            }

            set
            {
                this.SetPropertyValue(ref this.executable, value ?? string.Empty);
            }
        }

        [DataMember]
        public string CurrentDirectory
        {
            get
            {
                return this.currentDirectory;
            }

            set
            {
                this.SetPropertyValue(ref this.currentDirectory, value ?? string.Empty);
            }
        }

        [DataMember]
        public string Environment
        {
            get
            {
                return this.environment;
            }

            set
            {
                this.SetPropertyValue(ref this.environment, value ?? string.Empty);
            }
        }

        [DataMember]
        public string Aliases
        {
            get
            {
                return this.aliases;
            }

            set
            {
                this.SetPropertyValue(ref this.aliases, value ?? string.Empty);
            }
        }

        [DataMember]
        public string ColorTable
        {
            get
            {
                return this.colorTable;
            }

            set
            {
                this.SetPropertyValue(ref this.colorTable, value ?? string.Empty);
            }
        }
    }
}
