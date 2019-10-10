using System.Collections.Generic;
using System.Windows;

namespace DevPrompt.Api
{
    /// <summary>
    /// Adds menu items to the main window
    /// </summary>
    public interface IMenuItemProvider
    {
        IEnumerable<FrameworkElement> GetMenuItems(MenuType menu, IWindow window);
    }
}
