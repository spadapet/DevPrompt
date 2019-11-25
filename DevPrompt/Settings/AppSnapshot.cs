using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.UI.ViewModels;
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
    internal class AppSnapshot : PropertyNotifier
    {
        private ObservableCollection<WorkspaceSnapshot> workspaces;
        private Guid activeWorkspaceId;
        private static readonly object fileLock = new object();

        public AppSnapshot()
        {
            this.Initialize();
        }

        public AppSnapshot(Api.IWindow window, bool force = false)
            : this()
        {
            if (force || window.App.Settings.SaveTabsOnExit)
            {
                this.TakeSnapshot(window);
            }
        }

        public AppSnapshot(AppSnapshot copyFrom)
            : this()
        {
            this.CopyFrom(copyFrom);
        }

        public AppSnapshot Clone()
        {
            return new AppSnapshot(this);
        }

        public void CopyFrom(AppSnapshot copyFrom)
        {
            this.Workspaces.Clear();
            this.activeWorkspaceId = Guid.Empty;

            if (copyFrom != null)
            {
                this.activeWorkspaceId = copyFrom.activeWorkspaceId;

                foreach (WorkspaceSnapshot workspaceSnapshot in copyFrom.Workspaces)
                {
                    this.Workspaces.Add(workspaceSnapshot.Clone());
                }
            }
        }

        private void TakeSnapshot(Api.IWindow window)
        {
            foreach (IWorkspaceVM workspace in window.Workspaces.OfType<IWorkspaceVM>())
            {
                if (workspace.Snapshot is Api.IWorkspaceSnapshot workspaceSnapshot)
                {
                    if (window.ActiveWorkspace == workspace)
                    {
                        this.ActiveWorkspaceId = workspace.Id;
                    }

                    this.Workspaces.Add(new WorkspaceSnapshot(workspace.Id, workspaceSnapshot));
                }
            }
        }

        public Api.IWorkspaceSnapshot FindWorkspaceSnapshot(Guid id)
        {
            foreach (WorkspaceSnapshot snapshot in this.Workspaces)
            {
                if (snapshot.Id == id)
                {
                    return snapshot.Snapshot;
                }
            }

            return null;
        }

        [DataMember]
        public IList<WorkspaceSnapshot> Workspaces => this.workspaces;

        [DataMember]
        public Guid ActiveWorkspaceId
        {
            get => this.activeWorkspaceId;
            set => this.SetPropertyValue(ref this.activeWorkspaceId, value);
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.workspaces = new ObservableCollection<WorkspaceSnapshot>();
        }

        private static DataContractSerializer GetDataContractSerializer(App app)
        {
            List<Type> knownTypes = new List<Type>(AppSnapshot.CollectionTypes);
            DataContractSerializerSettings serializerSettings = new DataContractSerializerSettings()
            {
                KnownTypes = knownTypes.Distinct(),
                DataContractResolver = new SettingTypeResolver(app),
            };

            return new DataContractSerializer(typeof(AppSnapshot), serializerSettings);
        }

        private static IEnumerable<Type> CollectionTypes
        {
            get
            {
                yield return typeof(WorkspaceSnapshot);
            }
        }

        public static string DefaultSnapshotFile => Path.Combine(AppSettings.AppDataPath, Program.IsElevated ? "Snapshot.admin.xml" : "Snapshot.xml");

        public static async Task<AppSnapshot> Load(App app, string path)
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
                            return (AppSnapshot)AppSnapshot.GetDataContractSerializer(app).ReadObject(reader);
                        }
                    }
                });
            }
            catch
            {
            }

            return snapshot ?? new AppSnapshot();
        }

        public Task Save(App app)
        {
            return this.Save(app, AppSnapshot.DefaultSnapshotFile);
        }

        public Task Save(App app, string path)
        {
            AppSnapshot clone = this.Clone();

            Task task = Task.Run(() =>
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
                        AppSnapshot.GetDataContractSerializer(app).WriteObject(writer, clone);
                    }
                }
            });

            app.AddCriticalTask(task);
            return task;
        }
    }
}
