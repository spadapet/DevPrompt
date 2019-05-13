using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves/loads application settings
    /// </summary>
    [DataContract]
    internal class AppSettings : Api.PropertyNotifier, Api.IAppSettings
    {
        private ObservableCollection<ConsoleSettings> consoles;
        private ObservableCollection<GrabConsoleSettings> grabConsoles;
        private ObservableCollection<LinkSettings> links;
        private ObservableCollection<ToolSettings> tools;
        private Dictionary<string, object> customProperties;
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
            this.customProperties.Clear();

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

            foreach (KeyValuePair<string, object> pair in copyFrom.CustomProperties)
            {
                this.customProperties[pair.Key] = (pair.Value is ICloneable cloneable)
                    ? cloneable.Clone()
                    : pair.Value;
            }

            this.EnsureValid();
        }

        public static string AppDataPath
        {
            get
            {
                string exeFile = Assembly.GetExecutingAssembly().Location;
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
            foreach (VisualStudioSetup.Instance instance in await VisualStudioSetup.GetInstances())
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

        public static async Task<AppSettings> UnsafeLoad(App app, string path)
        {
            return await Task.Run(() =>
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
                        return (AppSettings)AppSettings.GetDataContractSerializer(app).ReadObject(reader);
                    }
                }
            });
        }

        public static async Task<AppSettings> Load(App app, string path)
        {
            AppSettings settings = null;

            try
            {
                if (File.Exists(path))
                {
                    settings = await AppSettings.UnsafeLoad(app, path);
                    settings.EnsureValid();
                }
            }
            catch
            {
            }

            if (settings == null)
            {
                settings = await AppSettings.GetDefaultSettings(DefaultSettingsFilter.All);
                await settings.Save(app, path);
            }

            return settings;
        }

        public Task<Exception> Save(App app, string path = null)
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
                    try
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            path = AppSettings.DefaultPath;
                        }

                        if (Directory.CreateDirectory(Path.GetDirectoryName(path)) != null)
                        {
                            using (Stream stream = File.Create(path))
                            using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                            {
                                AppSettings.GetDataContractSerializer(app).WriteObject(writer, clone);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                }

                return null;
            });
        }

        IEnumerable<Api.IConsoleSettings> Api.IAppSettings.ConsoleSettings => this.Consoles;

        public ObservableCollection<ConsoleSettings> ObservableConsoles => this.consoles;
        public ObservableCollection<GrabConsoleSettings> ObservableGrabConsoles => this.grabConsoles;
        public ObservableCollection<LinkSettings> ObservableLinks => this.links;
        public ObservableCollection<ToolSettings> ObservableTools => this.tools;

        [DataMember]
        public IList<ConsoleSettings> Consoles => this.consoles;

        [DataMember]
        public IList<GrabConsoleSettings> GrabConsoles => this.grabConsoles;

        [DataMember]
        public IList<LinkSettings> Links => this.links;

        [DataMember]
        public IList<ToolSettings> Tools => this.tools;

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

        [DataMember]
        public ICollection<KeyValuePair<string, object>> CustomProperties
        {
            get
            {
                return this.customProperties;
            }
        }

        public bool TryGetProperty<T>(string name, out T value)
        {
            if (!string.IsNullOrEmpty(name) && this.customProperties.TryGetValue(name, out object objectValue) && objectValue is T)
            {
                value = (T)objectValue;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public void SetProperty<T>(string name, T value)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                this.customProperties[name] = value;
                this.OnPropertyChanged($"Custom.{name}");
            }
        }

        public bool RemoveProperty(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && this.customProperties.Remove(name))
            {
                this.OnPropertyChanged($"Custom.{name}");
                return true;
            }

            return false;
        }

        string Api.IAppSettings.GetDefaultTabName(string path)
        {
            foreach (GrabConsoleSettings grab in this.GrabConsoles)
            {
                if (grab.CanGrab(path))
                {
                    return grab.TabName;
                }
            }

            return !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "Tab";
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.consoles = new ObservableCollection<ConsoleSettings>();
            this.grabConsoles = new ObservableCollection<GrabConsoleSettings>();
            this.links = new ObservableCollection<LinkSettings>();
            this.tools = new ObservableCollection<ToolSettings>();
            this.customProperties = new Dictionary<string, object>();
            this.saveTabsOnExit = true;
        }

        private static DataContractSerializer GetDataContractSerializer(App app)
        {
            List<Type> knownTypes = new List<Type>(AppSettings.CollectionTypes);
            DataContractSerializerSettings serializerSettings = new DataContractSerializerSettings()
            {
                KnownTypes = knownTypes.Distinct(),
                DataContractResolver = new SettingTypeResolver(app),
            };

            return new DataContractSerializer(typeof(AppSettings), serializerSettings);
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

        public void EnsureValid()
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
