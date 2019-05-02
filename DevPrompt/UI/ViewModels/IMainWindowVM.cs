using System;
using System.Collections.Generic;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// Public access to the main window
    /// </summary>
    public interface IMainWindowVM
    {
        IReadOnlyList<ITabVM> Tabs { get; }
        ITabVM ActiveTab { get; set; }
        ITabVM RestoreProcess(string state);
        void AddTab(ITabVM tab, bool activate);
        void RemoveTab(ITabVM tab);
        void SetError(Exception exception, string text = null);
    }
}
