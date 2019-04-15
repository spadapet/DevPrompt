using DevPrompt.Interop;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace DevPrompt.UI
{
    /// <summary>
    /// View model for each process tab (handles context menu items, etc)
    /// </summary>
    internal class ProcessVM : PropertyNotifier
    {
        private readonly MainWindowVM window;
        private string env;
        private string tabName;
        private string title;
        private bool active;
        private readonly Dictionary<string, string> envDict;

        public ProcessVM(MainWindowVM window, IProcess process)
        {
            this.window = window;
            this.envDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            this.Process = process;
            this.Hwnd = process.GetWindow();
            this.Env = string.Empty;
            this.Title = string.Empty;
            this.TabName = string.Empty;
        }

        public IProcess Process { get; }
        public IntPtr Hwnd { get; }

        public string Env
        {
            get
            {
                return this.env;
            }

            set
            {
                if (this.SetPropertyValue(ref this.env, value ?? string.Empty))
                {
                    this.envDict.Clear();

                    foreach (string entry in this.env.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] entry2 = entry.Split('=');
                        if (entry2.Length >= 2)
                        {
                            this.envDict[entry2[0]] = entry2[1];
                        }
                    }

                    this.OnPropertyChanged(nameof(this.ExpandedTabName));
                }
            }
        }

        public string GetEnv(string name)
        {
            if (this.envDict.TryGetValue(name, out string value))
            {
                return value;
            }

            return string.Empty;
        }

        public string ExpandEnv(string str)
        {
            if (str.IndexOf('%') != -1)
            {
                string[] strs = str.Split('%');
                StringBuilder sb = new StringBuilder(str.Length);

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

            return str;
        }

        public string TabName
        {
            get
            {
                return this.tabName;
            }

            set
            {
                if (this.SetPropertyValue(ref this.tabName, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.ExpandedTabName));
                }
            }
        }

        public string ExpandedTabName
        {
            get
            {
                return this.ExpandEnv(this.tabName);
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

        public bool Active
        {
            get
            {
                return this.active;
            }
        }

        public bool InternalActive
        {
            get
            {
                return this.active;
            }

            set
            {
                if (this.SetPropertyValue(ref this.active, value))
                {
                    if (this.active)
                    {
                        this.Process.Activate();
                    }
                    else
                    {
                        this.Process.Deactivate();
                    }

                    this.OnPropertyChanged(nameof(this.Active));
                }
            }
        }

        public ICommand ActivateCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.window.ActiveProcess = this;
                });
            }
        }

        public ICommand CloneCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.window.CloneProcess(this);
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.Process.Dispose();
                });
            }
        }

        public ICommand DetachCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.Process.Detach();
                });
            }
        }

        public ICommand DefaultsCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.Process.SystemCommandDefaults();
                });
            }
        }

        public ICommand PropertiesCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    this.Process.SystemCommandProperties();
                });
            }
        }

        public ICommand SetTabNameCommand
        {
            get
            {
                return new DelegateCommand((object arg) =>
                {
                    TabNameDialog dialog = new TabNameDialog(this)
                    {
                        Owner = this.window.Window
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        this.TabName = dialog.TabName;
                    }
                });
            }
        }
    }
}
