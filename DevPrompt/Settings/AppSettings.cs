using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves/loads application settings
    /// </summary>
    [DataContract]
    public class AppSettings : PropertyNotifier, ICloneable
    {
        private ObservableCollection<ConsoleSettings> consoles;
        private ObservableCollection<GrabConsoleSettings> grabConsoles;
        private ObservableCollection<LinkSettings> links;
        private ObservableCollection<ToolSettings> tools;
        private bool consoleGrabEnabled;
        private bool saveTabsOnExit;
        private static readonly object fileLock = new object();

        public AppSettings()
        {
            this.Initialize();
        }

        public AppSettings(AppSettings copyFrom)
        {
            this.Initialize();
            this.CopyFrom(copyFrom);
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public AppSettings Clone()
        {
            return new AppSettings(this);
        }

        public void CopyFrom(AppSettings copyFrom)
        {
            this.ConsoleGrabEnabled = copyFrom.ConsoleGrabEnabled;
            this.SaveTabsOnExit = copyFrom.SaveTabsOnExit;

            this.consoles.Clear();
            this.grabConsoles.Clear();
            this.links.Clear();
            this.tools.Clear();

            foreach (ConsoleSettings console in copyFrom.Consoles)
            {
                this.consoles.Add(console.Clone());
            }

            foreach (GrabConsoleSettings console in copyFrom.GrabConsoles)
            {
                this.grabConsoles.Add(console.Clone());
            }

            foreach (LinkSettings link in copyFrom.Links)
            {
                this.links.Add(link.Clone());
            }

            foreach (ToolSettings tool in copyFrom.Tools)
            {
                this.tools.Add(tool.Clone());
            }

            this.EnsureValid();
        }

        public static string AppDataPath
        {
            get
            {
                string exeFile = typeof(Program).Assembly.Location;
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(appDataPath, Path.GetFileNameWithoutExtension(exeFile));
            }
        }

        public static string DefaultPath
        {
            get
            {
                return Path.Combine(AppSettings.AppDataPath, "Settings.xml");
            }
        }

        [Flags]
        public enum DefaultSettingsFilter
        {
            None = 0,
            DevPrompts = 0x01,
            RawPrompts = 0x02,
            Grabs = 0x04,
            Links = 0x08,
            Tools = 0x10,
            All = 0xFF,
        }

        public static async Task<AppSettings> GetDefaultSettings(DefaultSettingsFilter filter)
        {
            AppSettings settings = new AppSettings();

            if ((filter & DefaultSettingsFilter.DevPrompts) != 0)
            {
                settings.AddVisualStudioEnlistments();
                await settings.AddVisualStudioDevPrompts();
            }

            if ((filter & DefaultSettingsFilter.RawPrompts) != 0)
            {
                settings.AddRawCommandPrompts();
            }

            if ((filter & DefaultSettingsFilter.Grabs) != 0)
            {
                settings.AddDefaultGrabs();
            }

            if ((filter & DefaultSettingsFilter.Links) != 0)
            {
                settings.AddDefaultLinks();
            }

            if ((filter & DefaultSettingsFilter.Tools) != 0)
            {
                settings.AddDefaultTools();
            }

            if (settings.consoles.Count > 0 && !settings.consoles.Any(c => c.RunAtStartup))
            {
                settings.consoles[0].RunAtStartup = true;
            }

            settings.EnsureValid();

            return settings;
        }

        private void AddVisualStudioEnlistments()
        {
            if (!Program.IsMicrosoftDomain)
            {
                return;
            }

            // Try to detect VS enlistments on all drives
            foreach (DriveInfo info in DriveInfo.GetDrives())
            {
                if (info.DriveType != DriveType.Fixed)
                {
                    continue;
                }

                for (int i = 0; i < 10; i++)
                {
                    string enlistment = Path.Combine(info.RootDirectory.FullName, "VS" + ((i == 0) ? string.Empty : i.ToString()));
                    if (Directory.Exists(enlistment) && File.Exists(Path.Combine(enlistment, "init.cmd")))
                    {
                        this.Consoles.Add(new ConsoleSettings()
                        {
                            MenuName = $"cmd.exe ({enlistment})",
                            TabName = $"%BaseDir,{enlistment.Substring(Path.GetPathRoot(enlistment).Length)}%, %_ParentBranch,...%",
                            StartingDirectory = enlistment,
                            Arguments = "/k init.cmd -skipexportsprune",
                        });
                    }
                }
            }
        }

        private async Task AddVisualStudioDevPrompts()
        {
            List<VisualStudioSetup.Instance> instances = new List<VisualStudioSetup.Instance>(await VisualStudioSetup.GetInstancesAsync());
            foreach (VisualStudioSetup.Instance instance in instances)
            {
                string file = Path.Combine(instance.Path, "Common7", "Tools", "VsDevCmd.bat");
                if (File.Exists(file))
                {
                    string name = instance.DisplayName.Split(' ')[0];
                    this.consoles.Add(new ConsoleSettings()
                    {
                        MenuName = $"VS prompt {name}",
                        TabName = $"VS {name}",
                        Arguments = $"/k \"{file}\"",
                    });
                }
            }
        }

        private void AddRawCommandPrompts()
        {
            this.consoles.Add(new ConsoleSettings()
            {
                ConsoleType = ConsoleType.Cmd,
                MenuName = "Raw cmd.exe",
                TabName = "Cmd",
            });

            this.consoles.Add(new ConsoleSettings()
            {
                ConsoleType = ConsoleType.PowerShell,
                MenuName = "Raw powershell.exe",
                TabName = "PowerShell",
            });
        }

        private void AddDefaultGrabs()
        {
            this.GrabConsoles.Add(new GrabConsoleSettings()
            {
                ExeName = "cmd.exe",
                TabName = "Cmd",
                TabActivate = true,
            });

            this.GrabConsoles.Add(new GrabConsoleSettings()
            {
                ExeName = "powershell.exe",
                TabName = "PowerShell",
                TabActivate = true,
            });
        }

        private void AddDefaultLinks()
        {
            this.Links.Add(new LinkSettings()
            {
                Name = "Azure DevOps",
                Address = "https://dev.azure.com",
            });

            this.Links.Add(new LinkSettings()
            {
                Name = "GitHub",
                Address = "https://github.com",
            });

            this.Links.Add(new LinkSettings()
            {
                Name = "Visual Studio",
                Address = "https://visualstudio.microsoft.com",
            });
        }

        private void AddDefaultTools()
        {
            this.Tools.Add(new ToolSettings()
            {
                Name = "Notepad",
                Command = "%windir%\\notepad.exe",
            });
        }

        public static async Task<AppSettings> Load(string path)
        {
            AppSettings settings = null;

            try
            {
                if (File.Exists(path))
                {
                    settings = await Task.Run(() =>
                    {
                        XmlReaderSettings xmlSettings = new XmlReaderSettings()
                        {
                            XmlResolver = null
                        };

                        lock (AppSettings.fileLock)
                        {
                            using (Stream stream = File.OpenRead(path))
                            using (XmlReader reader = XmlReader.Create(stream, xmlSettings))
                            {
                                return (AppSettings)AppSettings.DataContractSerializer.ReadObject(reader);
                            }
                        }
                    });

                    settings.EnsureValid();
                }
            }
            catch
            {
            }

            if (settings == null)
            {
                settings = await AppSettings.GetDefaultSettings(DefaultSettingsFilter.All);
                await settings.Save(path);
            }

            return settings;
        }

        public Task Save()
        {
            return this.Save(AppSettings.DefaultPath);
        }

        public Task Save(string path)
        {
            AppSettings clone = this.Clone();

            return Task.Run(() =>
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings()
                {
                    Indent = true,
                };

                lock (AppSettings.fileLock)
                {
                    if (Directory.CreateDirectory(Path.GetDirectoryName(path)) != null)
                    {
                        using (Stream stream = File.Create(path))
                        using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                        {
                            AppSettings.DataContractSerializer.WriteObject(writer, clone);
                        }
                    }
                }
            });
        }

        [DataMember]
        public IList<ConsoleSettings> Consoles
        {
            get
            {
                return this.consoles;
            }
        }

        [DataMember]
        public IList<GrabConsoleSettings> GrabConsoles
        {
            get
            {
                return this.grabConsoles;
            }
        }

        [DataMember]
        public IList<LinkSettings> Links
        {
            get
            {
                return this.links;
            }
        }

        [DataMember]
        public IList<ToolSettings> Tools
        {
            get
            {
                return this.tools;
            }
        }

        [DataMember]
        public bool ConsoleGrabEnabled
        {
            get
            {
                return this.consoleGrabEnabled;
            }

            set
            {
                this.SetPropertyValue(ref this.consoleGrabEnabled, value);
            }
        }

        [DataMember]
        public bool SaveTabsOnExit
        {
            get
            {
                return this.saveTabsOnExit;
            }

            set
            {
                this.SetPropertyValue(ref this.saveTabsOnExit, value);
            }
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default)
        {
            this.consoles = new ObservableCollection<ConsoleSettings>();
            this.grabConsoles = new ObservableCollection<GrabConsoleSettings>();
            this.links = new ObservableCollection<LinkSettings>();
            this.tools = new ObservableCollection<ToolSettings>();
            this.saveTabsOnExit = true;
        }

        private static DataContractSerializer DataContractSerializer
        {
            get
            {
                return new DataContractSerializer(typeof(AppSettings), AppSettings.CollectionTypes);
            }
        }

        private static IEnumerable<Type> CollectionTypes
        {
            get
            {
                yield return typeof(ConsoleSettings);
                yield return typeof(GrabConsoleSettings);
                yield return typeof(LinkSettings);
                yield return typeof(ToolSettings);
            }
        }

        private void EnsureValid()
        {
            if (this.consoles.Count == 0)
            {
                // Have to have at least one console that can be created
                this.consoles.Add(new ConsoleSettings()
                {
                    RunAtStartup = true
                });
            }
        }
    }
}
