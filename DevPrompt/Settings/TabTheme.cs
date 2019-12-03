using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace DevPrompt.Settings
{
    [DataContract]
    internal sealed class TabTheme : Api.Utility.PropertyNotifier, Api.ITabTheme, Api.ITabThemeKey, IEquatable<TabTheme>
    {
        public Brush ForegroundSelectedBrush => this.cachedBrushes.Value.ForegroundSelectedBrush;
        public Brush BackgroundSelectedBrush => this.cachedBrushes.Value.BackgroundSelectedBrush;
        public Brush ForegroundUnselectedBrush => this.cachedBrushes.Value.ForegroundUnselectedBrush;
        public Brush BackgroundUnselectedBrush => this.cachedBrushes.Value.BackgroundUnselectedBrush;

        private Lazy<CachedBrushes> cachedBrushes;
        private Color themeKeyColor;

        public TabTheme()
        {
            this.Initialize();
        }

        public TabTheme(Color themeKeyColor)
            : this()
        {
            this.themeKeyColor = themeKeyColor;
        }

        public TabTheme(TabTheme copyFrom)
            : this()
        {
            this.themeKeyColor = copyFrom.themeKeyColor;
        }

        public bool Equals(TabTheme other)
        {
            return this.themeKeyColor == other.themeKeyColor;
        }

        public TabTheme Clone()
        {
            return new TabTheme(this);
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default)
        {
            this.cachedBrushes = new Lazy<CachedBrushes>(() => new CachedBrushes(this.themeKeyColor));
        }

        public Color ThemeKeyColor
        {
            get => this.themeKeyColor;
            set
            {
                if (this.SetPropertyValue(ref this.themeKeyColor, value, null))
                {
                    if (this.cachedBrushes.IsValueCreated)
                    {
                        this.cachedBrushes = new Lazy<CachedBrushes>(() => new CachedBrushes(value));
                    }

                    this.OnPropertiesChanged();
                }
            }
        }

        [DataMember]
        public string ThemeKeyColorString
        {
            get => WpfUtility.ColorToString(this.ThemeKeyColor);
            set => this.ThemeKeyColor = WpfUtility.ColorFromString(value);
        }

        public static IEnumerable<TabTheme> DefaultTabThemes
        {
            get
            {
                yield return new TabTheme(default(Color));
                yield return new TabTheme(Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4)); // gray
                yield return new TabTheme(Color.FromArgb(0xFF, 0x9D, 0x9D, 0x9D)); // gray
                yield return new TabTheme(Color.FromArgb(0xFF, 0x4B, 0x4B, 0x4B)); // gray
                yield return new TabTheme(Color.FromArgb(0xFF, 0xF9, 0xD3, 0x81)); // yellow
                yield return new TabTheme(Color.FromArgb(0xFF, 0xEA, 0xAF, 0x4D)); // orange
                yield return new TabTheme(Color.FromArgb(0xFF, 0xFF, 0x8F, 0x83)); // red
                yield return new TabTheme(Color.FromArgb(0xFF, 0xAE, 0x39, 0x34)); // red
                yield return new TabTheme(Color.FromArgb(0xFF, 0x9A, 0xD1, 0xF9)); // blue
                yield return new TabTheme(Color.FromArgb(0xFF, 0x58, 0xAE, 0xEE)); // blue
                yield return new TabTheme(Color.FromArgb(0xFF, 0x8D, 0xED, 0xA7)); // green
                yield return new TabTheme(Color.FromArgb(0xFF, 0x44, 0xC5, 0x5B)); // green
                yield return new TabTheme(Color.FromArgb(0xFF, 0xC3, 0xA7, 0xE1)); // purple
                yield return new TabTheme(Color.FromArgb(0xFF, 0x95, 0x69, 0xC8)); // purple
                yield return new TabTheme(Color.FromArgb(0xFF, 0xBA, 0xB5, 0xAA)); // brown
                yield return new TabTheme(Color.FromArgb(0xFF, 0x94, 0x8E, 0x82)); // brown
            }
        }

        private sealed class CachedBrushes
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
