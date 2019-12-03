using DevPrompt.ProcessWorkspace.UI.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DevPrompt.ProcessWorkspace.Settings
{
    /// <summary>
    /// Saves the state of a process workspace during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    internal sealed class ProcessWorkspaceSnapshot : Api.Utility.PropertyNotifier, Api.IWorkspaceSnapshot
    {
        private ObservableCollection<Api.ITabSnapshot> tabs;
        private int activeTabIndex;

        public ProcessWorkspaceSnapshot()
        {
            this.Initialize();
        }

        public ProcessWorkspaceSnapshot(Api.ITabWorkspace workspace)
            : this()
        {
            foreach (TabVM tab in workspace.Tabs.OfType<TabVM>())
            {
                if (tab.Snapshot is Api.ITabSnapshot tabSnapshot)
                {
                    if (workspace.ActiveTab == tab)
                    {
                        this.activeTabIndex = this.tabs.Count;
                    }

                    this.tabs.Add(tabSnapshot);
                }
            }
        }

        public ProcessWorkspaceSnapshot(ProcessWorkspaceSnapshot copyFrom)
            : this()
        {
            this.tabs.Clear();

            if (copyFrom != null)
            {
                this.activeTabIndex = copyFrom.activeTabIndex;

                foreach (Api.ITabSnapshot tab in copyFrom.Tabs)
                {
                    this.tabs.Add(tab.Clone());
                }
            }
        }

        public Api.IWorkspaceSnapshot Clone()
        {
            return new ProcessWorkspaceSnapshot(this);
        }

        public Api.IWorkspace Restore(Api.IWindow window)
        {
            return new ProcessWorkspace(window, this);
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default)
        {
            this.tabs = new ObservableCollection<Api.ITabSnapshot>();
        }

        [DataMember]
        public IList<Api.ITabSnapshot> Tabs => this.tabs;

        [DataMember]
        public int ActiveTabIndex
        {
            get => this.activeTabIndex;
            set => this.SetPropertyValue(ref this.activeTabIndex, value);
        }
    }
}
