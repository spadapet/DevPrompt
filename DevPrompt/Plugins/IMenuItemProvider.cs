using DevPrompt.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace DevPrompt.Plugins
{
    /// <summary>
    /// Plugins implement this to add menu items
    /// </summary>
    public interface IMenuItemProvider
    {
        IEnumerable<MenuItem> GetMenuItems(MenuType menu, IMainWindowVM window);
    }
}
