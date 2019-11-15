using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DevPrompt.Settings
{
    internal class TabTheme : Api.ITabTheme, Api.ITabThemeKey
    {
        public Brush ForegroundSelectedBrush => this.cachedBrushes.Value.ForegroundSelectedBrush;
        public Brush BackgroundSelectedBrush => this.cachedBrushes.Value.BackgroundSelectedBrush;
        public Brush ForegroundUnselectedBrush => this.cachedBrushes.Value.ForegroundUnselectedBrush;
        public Brush BackgroundUnselectedBrush => this.cachedBrushes.Value.BackgroundUnselectedBrush;
        public Color KeyColor { get; }

        private readonly Lazy<CachedBrushes> cachedBrushes;

        public TabTheme(Color keyColor)
        {
            this.KeyColor = keyColor;
            this.cachedBrushes = new Lazy<CachedBrushes>(() => new CachedBrushes(keyColor));
        }

        public static IEnumerable<string> DefaultTabThemeStringKeys
        {
            get
            {
                yield return "#00000000";
                yield return "#FFD4D4D4"; // gray
                yield return "#FF9D9D9D"; // gray
                yield return "#FF4B4B4B"; // gray
                yield return "#FFF9D381"; // yellow
                yield return "#FFEAAF4D"; // orange
                yield return "#FFFF8F83"; // red
                yield return "#FFAE3934"; // red
                yield return "#FF9AD1F9"; // blue
                yield return "#FF58AEEE"; // blue
                yield return "#FF8DEDA7"; // green
                yield return "#FF44C55B"; // green
                yield return "#FFC3A7E1"; // purple
                yield return "#FF9569C8"; // purple
                yield return "#FFBAB5AA"; // brown
                yield return "#FF948E82"; // brown
            }
        }

        private class CachedBrushes
        {
            public Brush ForegroundSelectedBrush { get; }
            public Brush ForegroundUnselectedBrush { get; }
            public Brush BackgroundSelectedBrush { get; }
            public Brush BackgroundUnselectedBrush { get; }

            public CachedBrushes(Color backgroundColor)
            {
                if (backgroundColor != default)
                {
                    Color foregroundColor = ((backgroundColor.R * 0.299 + backgroundColor.G * 0.587 + backgroundColor.B * 0.114) > 100.0)
                        ? Colors.Black
                        : Colors.White;

                    Color fadedBackgroundColor = backgroundColor;
                    fadedBackgroundColor.A = 0x40;

                    this.ForegroundSelectedBrush = new SolidColorBrush(foregroundColor);
                    this.ForegroundUnselectedBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.675 };
                    this.BackgroundSelectedBrush = new SolidColorBrush(backgroundColor);

                    // new SolidColorBrush(backgroundColor) { Opacity = 0.25 };
                    this.BackgroundUnselectedBrush = new LinearGradientBrush(new GradientStopCollection(new GradientStop[]
                    {
                        new GradientStop(Colors.Transparent, 0.0),
                        new GradientStop(Colors.Transparent, 0.875),
                        new GradientStop(backgroundColor, 0.875),
                        new GradientStop(backgroundColor, 1.0),
                    }), 90.0);
                }
            }
        }
    }
}
