using DevPrompt.ProcessWorkspace.UI.ViewModels;
using DevPrompt.ProcessWorkspace.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DevPrompt.ProcessWorkspace.Settings
{
    [DataContract]
    internal abstract class TabWorkspaceSnapshot : PropertyNotifier, Api.IWorkspaceSnapshot
    {
        private ObservableCollection<Api.ITabSnapshot> tabs;
        private int activeTabIndex;

        public TabWorkspaceSnapshot()
        {
            this.Initialize();
        }

        public TabWorkspaceSnapshot(Api.ITabWorkspace workspace)
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

        public TabWorkspaceSnapshot(TabWorkspaceSnapshot copyFrom)
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

        public abstract Api.IWorkspaceSnapshot Clone();
        public abstract Api.IWorkspace Restore(Api.IWindow window);

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
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
