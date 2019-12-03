using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Caches information about an installed plugin
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Id}, {Version}")]
    internal sealed class InstalledPluginInfo : Api.Utility.PropertyNotifier
    {
        private string id;
        private string version;
        private string rootPath;
        private Dictionary<string, List<InstalledPluginAssemblyInfo>> assemblies;

        public InstalledPluginInfo()
        {
            this.Initialize();
        }

        public InstalledPluginInfo(InstalledPluginInfo copyFrom)
        {
            this.Initialize();
            this.CopyFrom(copyFrom);
        }

        public void CopyFrom(InstalledPluginInfo copyFrom)
        {
            this.Id = copyFrom.Id;
            this.Version = copyFrom.Version;
            this.RootPath = copyFrom.RootPath;
            this.Assemblies.Clear();

            foreach (KeyValuePair<string, List<InstalledPluginAssemblyInfo>> pair in copyFrom.Assemblies)
            {
                this.Assemblies.Add(pair.Key, pair.Value.Select(v => v.Clone()).ToList());
            }

            this.OnPropertyChanged(nameof(this.Assemblies));
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.id = string.Empty;
            this.version = string.Empty;
            this.rootPath = string.Empty;
            this.assemblies = new Dictionary<string, List<InstalledPluginAssemblyInfo>>(StringComparer.OrdinalIgnoreCase);
        }

        public InstalledPluginInfo Clone()
        {
            return new InstalledPluginInfo(this);
        }

        public IEnumerable<string> PluginContainerFiles
        {
            get
            {
                foreach (List<InstalledPluginAssemblyInfo> list in this.assemblies.Values)
                {
                    foreach (InstalledPluginAssemblyInfo info in list)
                    {
                        if (info.IsContainer)
                        {
                            yield return info.Path;
                        }
                    }
                }
            }
        }

        [DataMember]
        public string Id
        {
            get => this.id;
            set => this.SetPropertyValue(ref this.id, value ?? string.Empty);
        }

        [DataMember]
        public string Version
        {
            get => this.version;
            set => this.SetPropertyValue(ref this.version, value ?? string.Empty);
        }

        [DataMember]
        public string RootPath
        {
            get => this.rootPath;
            set => this.SetPropertyValue(ref this.rootPath, value ?? string.Empty);
        }

        [DataMember]
        public IDictionary<string, List<InstalledPluginAssemblyInfo>> Assemblies => this.assemblies;

        public async Task GatherAssemblyInfo()
        {
            this.Assemblies.Clear();
            string rootPath = this.RootPath;

            List<InstalledPluginAssemblyInfo> assembliesList = await Task.Run(() =>
            {
                List<InstalledPluginAssemblyInfo> assembliesResult = new List<InstalledPluginAssemblyInfo>();
                string[] files = Array.Empty<string>();

                try
                {
                    files = Directory.GetFiles(rootPath, "*.dll", SearchOption.AllDirectories);
                }
                catch
                {
                    Debug.Fail("Failure in GatherAssemblyPaths, Directory.GetFiles");
                }

                foreach (string file in files)
                {
                    try
                    {
                        AssemblyName name = AssemblyName.GetAssemblyName(file);
                        assembliesResult.Add(new InstalledPluginAssemblyInfo()
                        {
                            AssemblyName = name,
                            Path = file,
                            IsContainer = file.EndsWith(PluginState.DllSuffix, StringComparison.OrdinalIgnoreCase),
                        });
                    }
                    catch
                    {
                        Debug.Fail("Failure in GatherAssemblyPaths, AssemblyName.GetAssemblyName");
                    }
                }

                return assembliesResult;
            });

            foreach (InstalledPluginAssemblyInfo info in assembliesList)
            {
                if (!this.Assemblies.TryGetValue(info.AssemblyName.Name, out List<InstalledPluginAssemblyInfo> list))
                {
                    list = new List<InstalledPluginAssemblyInfo>();
                    this.Assemblies.Add(info.AssemblyName.Name, list);
                }

                list.Add(info);
            }

            this.OnPropertyChanged(nameof(this.Assemblies));
        }

        public static InstalledPluginAssemblyInfo FindBestAssemblyMatch(AssemblyName findName, IEnumerable<InstalledPluginInfo> pluginInfos)
        {
            InstalledPluginAssemblyInfo bestInfo = null;

            foreach (InstalledPluginInfo pluginInfo in pluginInfos)
            {
                if (pluginInfo.Assemblies.TryGetValue(findName.Name, out List<InstalledPluginAssemblyInfo> list))
                {
                    foreach (InstalledPluginAssemblyInfo checkInfo in list)
                    {
                        bool matchesExceptVersion = false;

                        if (string.Equals(findName.CultureName, checkInfo.AssemblyName.CultureName, StringComparison.OrdinalIgnoreCase))
                        {
                            byte[] pk1 = findName.GetPublicKeyToken();
                            byte[] pk2 = checkInfo.AssemblyName.GetPublicKeyToken();

                            if (pk1 == pk2)
                            {
                                matchesExceptVersion = true;
                            }
                            else if (pk1 != null && pk2 != null)
                            {
                                matchesExceptVersion = pk1.SequenceEqual(pk2);
                            }
                        }

                        if (matchesExceptVersion)
                        {
                            if (findName.Version == checkInfo.AssemblyName.Version)
                            {
                                // Everything matches
                                return checkInfo;
                            }
                            else if (checkInfo.AssemblyName.Version >= findName.Version)
                            {
                                // The best version is the lowest version (least likely to change APIs)
                                if (bestInfo == null || checkInfo.AssemblyName.Version < bestInfo.AssemblyName.Version)
                                {
                                    bestInfo = checkInfo;
                                }
                            }
                        }
                    }
                }
            }

            return bestInfo;
        }
    }
}
