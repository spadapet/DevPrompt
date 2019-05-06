using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// Public access to the main window
    /// </summary>
    public interface IMainWindowVM
    {
        Dispatcher Dispatcher { get; }
        void StartProcess(string path, string arguments = null);

        // Tabs
        IReadOnlyList<ITabVM> Tabs { get; }
        ITabVM ActiveTab { get; set; }
        ITabVM RestoreProcess(string state);
        void AddTab(ITabVM tab, bool activate);
        void RemoveTab(ITabVM tab);

        // State
        void SetError(Exception exception, string text = null);
        IDisposable BeginLoading(Action cancelAction = null, string text = null);
        void CancelLoading();
        bool Loading { get; }
        bool NotLoading { get; }
    }
}
