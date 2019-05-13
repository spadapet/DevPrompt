using System.Collections.Generic;
using System.Windows.Controls;

namespace DevPrompt.Api
{
    /// <summary>
    /// Adds menu items to the main window
    /// </summary>
    public interface IMenuItemProvider
    {
        IEnumerable<MenuItem> GetMenuItems(MenuType menu, IWindow window);
    }
}
