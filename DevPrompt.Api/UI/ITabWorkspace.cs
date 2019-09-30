using System.Collections.Generic;

namespace DevPrompt.Api
{
    public interface ITabWorkspace : IWorkspace
    {
        IEnumerable<ITabHolder> Tabs { get; }
        ITabHolder ActiveTab { get; set; }
        ITabHolder AddTab(ITab tab, bool activate);
        ITabHolder AddTab(ITabSnapshot snapshot, bool activate);
        void RemoveTab(ITabHolder tab);

        void TabCycleStop();
        void TabCycleNext();
        void TabCyclePrev();
        void TabContextMenu();
    }
}
