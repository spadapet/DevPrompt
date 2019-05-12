using DevPrompt.Plugins;
using DevPrompt.UI.ViewModels;
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
    /// Saves the state of the app during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    public class AppSnapshot : PropertyNotifier, ICloneable
    {
        private ObservableCollection<ITabSnapshot> tabs;
        private int activeTabIndex;
        private static readonly object fileLock = new object();

        public AppSnapshot()
            : this((MainWindowVM)null)
        {
        }

        internal AppSnapshot(MainWindowVM window, bool force = false)
        {
            this.Initialize();

            if (window != null && (force || window.AppSettings.SaveTabsOnExit))
            {
                foreach (ITabVM tab in window.Tabs)
                {
                    if (tab.Snapshot is ITabSnapshot tabSnapshot)
                    {
                        if (tab.Active)
                        {
                            this.activeTabIndex = this.tabs.Count;
                        }

                        this.tabs.Add(tabSnapshot);
                    }
                }
            }
        }

        public AppSnapshot(AppSnapshot copyFrom)
        {
            this.Initialize();
            this.CopyFrom(copyFrom);
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public AppSnapshot Clone()
        {
            return new AppSnapshot(this);
        }

        public void CopyFrom(AppSnapshot copyFrom)
        {
            this.tabs.Clear();
            this.activeTabIndex = copyFrom.activeTabIndex;

            if (copyFrom != null)
            {
                foreach (ITabSnapshot tab in copyFrom.Tabs)
                {
                    this.tabs.Add(tab.Clone());
                }
            }
        }

        [DataMember]
        public IList<ITabSnapshot> Tabs
        {
            get
            {
                return this.tabs;
            }
        }

        [DataMember]
        public int ActiveTabIndex
        {
            get
            {
                return this.activeTabIndex;
            }

            set
            {
                this.SetPropertyValue(ref this.activeTabIndex, value);
            }
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.tabs = new ObservableCollection<ITabSnapshot>();
        }

        private static DataContractSerializer GetDataContractSerializer(IApp pluginApp)
        {
            List<Type> knownTypes = new List<Type>(AppSnapshot.CollectionTypes);

            foreach (ISettingTypes types in pluginApp?.GetExports<ISettingTypes>() ?? Enumerable.Empty<ISettingTypes>())
            {
                foreach (Type type in types.SnapshotTypes ?? Enumerable.Empty<Type>())
                {
                    if (type != null && !knownTypes.Contains(type))
                    {
                        knownTypes.Add(type);
                    }
                }
            }

            return new DataContractSerializer(typeof(AppSnapshot), knownTypes);
        }

        private static IEnumerable<Type> CollectionTypes
        {
            get
            {
                yield return typeof(ConsoleSnapshot);
            }
        }

        public static string DefaultPath
        {
            get
            {
                return Path.Combine(AppSettings.AppDataPath, "Snapshot.xml");
            }
        }

        public static async Task<AppSnapshot> Load(IApp pluginApp, string path)
        {
            AppSnapshot snapshot = null;

            try
            {
                snapshot = await Task.Run(() =>
                {
                    XmlReaderSettings xmlSettings = new XmlReaderSettings()
                    {
                        XmlResolver = null
                    };

                    lock (AppSnapshot.fileLock)
                    {
                        using (Stream stream = File.OpenRead(path))
                        using (XmlReader reader = XmlReader.Create(stream, xmlSettings))
                        {
                            return (AppSnapshot)AppSnapshot.GetDataContractSerializer(pluginApp).ReadObject(reader);
                        }
                    }
                });
            }
            catch
            {
            }

            return snapshot ?? new AppSnapshot();
        }

        public Task Save(IApp pluginApp)
        {
            return this.Save(pluginApp, AppSnapshot.DefaultPath);
        }

        public Task Save(IApp pluginApp, string path)
        {
            AppSnapshot clone = this.Clone();

            return Task.Run(() =>
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings()
                {
                    Indent = true,
                };

                lock (AppSnapshot.fileLock)
                {
                    using (Stream stream = File.Create(path))
                    using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                    {
                        AppSnapshot.GetDataContractSerializer(pluginApp).WriteObject(writer, clone);
                    }
                }
            });
        }
    }
}
