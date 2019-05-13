using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DevPrompt.Api
{
    [DataContract]
    public abstract class TabWorkspaceSnapshot : PropertyNotifier, IWorkspaceSnapshot
    {
        private ObservableCollection<ITabSnapshot> tabs;
        private int activeTabIndex;

        public TabWorkspaceSnapshot()
        {
            this.Initialize();
        }

        public TabWorkspaceSnapshot(ITabWorkspace workspace)
            : this()
        {
            foreach (ITabVM tab in workspace?.Tabs ?? Enumerable.Empty<ITabVM>())
            {
                if (tab.Snapshot is ITabSnapshot tabSnapshot)
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

                foreach (ITabSnapshot tab in copyFrom.Tabs)
                {
                    this.tabs.Add(tab.Clone());
                }
            }
        }

        public abstract IWorkspaceSnapshot Clone();
        public abstract IWorkspace Restore(IWindow window);

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.tabs = new ObservableCollection<ITabSnapshot>();
        }

        [DataMember]
        public IList<ITabSnapshot> Tabs => this.tabs;

        [DataMember]
        public int ActiveTabIndex
        {
            get => this.activeTabIndex;
            set => this.SetPropertyValue(ref this.activeTabIndex, value);
        }
    }
}
