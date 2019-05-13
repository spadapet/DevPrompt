using System;
using System.Collections.Generic;
using System.Text;

namespace DevPrompt.Interop
{
    /// <summary>
    /// Wrapper for native processes
    /// </summary>
    internal class NativeProcess : Api.PropertyNotifier, Api.IProcess
    {
        public IProcess Process { get; }
        public IntPtr Hwnd { get; }
        private string title;
        private string environment;
        private readonly Dictionary<string, string> envDict;

        public NativeProcess(IProcess process)
        {
            this.Process = process;
            this.Hwnd = process.GetWindow();
            this.title = string.Empty;
            this.environment = string.Empty;
            this.envDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            this.Process.Dispose();
        }

        public void Detach()
        {
            this.Process.Detach();
        }

        public void Activate()
        {
            this.Process.Activate();
        }

        public void Deactivate()
        {
            this.Process.Deactivate();
        }

        public string State
        {
            get
            {
                return this.Process.GetState() ?? string.Empty;
            }
        }

        public string Title
        {
            get
            {
                return this.title;
            }

            set
            {
                this.SetPropertyValue(ref this.title, value ?? string.Empty);
            }
        }

        public string Environment
        {
            get
            {
                return this.environment;
            }

            set
            {
                if (this.SetPropertyValue(ref this.environment, value ?? string.Empty, null))
                {
                    this.envDict.Clear();

                    foreach (string entry in this.environment.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] entry2 = entry.Split('=');
                        if (entry2.Length >= 2)
                        {
                            this.envDict[entry2[0]] = entry2[1];
                        }
                    }

                    this.OnPropertyChanged(nameof(this.Environment));
                }
            }
        }

        private string GetEnv(string name)
        {
            if (this.envDict.TryGetValue(name, out string value))
            {
                return value;
            }

            return string.Empty;
        }

        public string ExpandEnvironmentVariables(string text)
        {
            if (text.IndexOf('%') != -1)
            {
                string[] strs = text.Split('%');
                StringBuilder sb = new StringBuilder(text.Length);

                for (int i = 0; i < strs.Length; i++)
                {
                    if ((i % 2) == 0)
                    {
                        sb.Append(strs[i]);
                    }
                    else
                    {
                        string[] strs2 = strs[i].Split(',');
                        string value = this.GetEnv(strs2[0]);

                        if (strs2.Length == 1)
                        {
                            sb.Append(value);
                        }
                        else if (strs2.Length == 2)
                        {
                            sb.Append(!string.IsNullOrEmpty(value) ? value : strs2[1]);
                        }
                    }
                }

                return sb.ToString();
            }

            return text;
        }

        public void Focus()
        {
            this.Process.Focus();
        }

        public void RunCommand(Api.ProcessCommand command)
        {
            switch (command)
            {
                case Api.ProcessCommand.DefaultsDialog:
                    this.Process.SystemCommandDefaults();
                    break;

                case Api.ProcessCommand.PropertiesDialog:
                    this.Process.SystemCommandProperties();
                    break;
            }
        }

        public override int GetHashCode()
        {
            return this.Hwnd.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is NativeProcess other && this.Hwnd == other.Hwnd;
        }
    }
}
