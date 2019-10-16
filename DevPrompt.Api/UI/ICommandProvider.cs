using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.Api
{
    /// <summary>
    /// Adds commands to the main window
    /// </summary>
    public interface ICommandProvider
    {
        IEnumerable<FrameworkElement> GetMenuItems(MenuType menu, IWindow window);
        IEnumerable<KeyBinding> GetKeyBindings(IWindow window);
    }
}
