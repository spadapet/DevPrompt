using System.Windows.Media;

namespace DevPrompt.Api
{
    /// <summary>
    /// Tabs that can change color can implement this
    /// </summary>
    public interface ITabTheme
    {
        Brush ForegroundSelectedBrush { get; }
        Brush BackgroundSelectedBrush { get; }
        Brush ForegroundUnselectedBrush { get; }
        Brush BackgroundUnselectedBrush { get; }
    }
}
