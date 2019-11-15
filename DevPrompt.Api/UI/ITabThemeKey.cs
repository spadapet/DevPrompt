using System.Windows.Media;

namespace DevPrompt.Api
{
    /// <summary>
    /// Tabs that get their colors from a theme key color can implement this
    /// </summary>
    public interface ITabThemeKey
    {
        Color KeyColor { get; }
    }
}
