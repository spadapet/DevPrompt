using System.Collections.Generic;

namespace DevPrompt.Api
{
    public interface ITabWorkspace : IWorkspace
    {
        IEnumerable<ITabVM> Tabs { get; }
        ITabVM ActiveTab { get; set; }
        void AddTab(ITabVM tab, bool activate);
        void RemoveTab(ITabVM tab);

        void TabCycleStop();
        void TabCycleNext();
        void TabCyclePrev();
    }
}
