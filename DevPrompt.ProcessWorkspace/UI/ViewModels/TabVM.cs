using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.UI.ViewModels
{
    internal class TabVM
        : PropertyNotifier
        , Api.ITabHolder
        , Api.ITabTheme
        , Api.ITabThemeKey
        , IDisposable
    {
        public string Title => this.Tab?.Title ?? string.Empty;
        public UIElement ViewElement => this.Tab?.ViewElement;
        public bool CreatedTab => this.tab != null;
        public IEnumerable<FrameworkElement> ContextMenuItems => this.tab?.ContextMenuItems ?? Enumerable.Empty<FrameworkElement>();

        private readonly Api.IWindow window;
        private readonly Api.ITabWorkspace workspace;
        private Api.ITab tab;
        private Api.ITabSnapshot snapshot;
        private Api.ActiveState activeState;
        private bool restoring;

        private TabVM(Api.IWindow window, Api.ITabWorkspace workspace)
        {
            this.window = window;
            this.workspace = workspace;
        }

        public TabVM(Api.IWindow window, Api.ITabWorkspace workspace, Api.ITab tab)
            : this(window, workspace)
        {
            this.InitTab(tab);
        }

        public TabVM(Api.IWindow window, Api.ITabWorkspace workspace, Api.ITabSnapshot snapshot)
            : this(window, workspace)
        {
            this.snapshot = snapshot;
        }

        public void Dispose()
        {
            if (this.tab != null)
            {
                this.tab.PropertyChanged -= this.OnTabPropertyChanged;
                (this.tab as IDisposable)?.Dispose();
            }
        }

        private void InitTab(Api.ITab tab)
        {
            Debug.Assert(this.tab == null || this.tab == tab);
            if (this.tab == null)
            {
                this.tab = tab;

                if (this.tab != null)
                {
                    this.tab.PropertyChanged += this.OnTabPropertyChanged;
                }

                this.OnPropertyChanged(nameof(this.Tab));
                this.OnPropertyChanged(nameof(this.Name));
                this.OnPropertyChanged(nameof(this.Tooltip));
                this.OnPropertyChanged(nameof(this.ContextMenuItems));
                this.OnPropertyChanged(nameof(this.ForegroundSelectedBrush));
                this.OnPropertyChanged(nameof(this.ForegroundUnselectedBrush));
                this.OnPropertyChanged(nameof(this.BackgroundSelectedBrush));
                this.OnPropertyChanged(nameof(this.BackgroundUnselectedBrush));
            }
        }

        private void OnTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            bool all = string.IsNullOrEmpty(args.PropertyName);

            if (all || args.PropertyName == nameof(Api.ITab.Name))
            {
                this.OnPropertyChanged(nameof(this.Name));
            }

            if (all || args.PropertyName == nameof(Api.ITab.Title))
            {
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (all || args.PropertyName == nameof(Api.ITab.Tooltip))
            {
                this.OnPropertyChanged(nameof(this.Tooltip));
            }

            if (all || args.PropertyName == nameof(Api.ITab.ViewElement))
            {
                this.OnPropertyChanged(nameof(this.ViewElement));
            }

            if (all || args.PropertyName == nameof(Api.ITab.ContextMenuItems))
            {
                this.OnPropertyChanged(nameof(this.ContextMenuItems));
            }

            if (all || args.PropertyName == nameof(Api.ITabTheme.BackgroundSelectedBrush))
            {
                this.OnPropertyChanged(nameof(this.BackgroundSelectedBrush));
            }

            if (all || args.PropertyName == nameof(Api.ITabTheme.BackgroundUnselectedBrush))
            {
                this.OnPropertyChanged(nameof(this.BackgroundUnselectedBrush));
            }

            if (all || args.PropertyName == nameof(Api.ITabTheme.ForegroundSelectedBrush))
            {
                this.OnPropertyChanged(nameof(this.ForegroundSelectedBrush));
            }

            if (all || args.PropertyName == nameof(Api.ITabTheme.ForegroundUnselectedBrush))
            {
                this.OnPropertyChanged(nameof(this.ForegroundUnselectedBrush));
            }
        }

        public Brush BackgroundSelectedBrush => this.GetThemeBrush(c => c.BackgroundSelectedBrush);
        public Brush BackgroundUnselectedBrush => this.GetThemeBrush(c => c.BackgroundUnselectedBrush);
        public Brush ForegroundSelectedBrush => this.GetThemeBrush(c => c.ForegroundSelectedBrush);
        public Brush ForegroundUnselectedBrush => this.GetThemeBrush(c => c.ForegroundUnselectedBrush);

        public Color ThemeKeyColor
        {
            get
            {
                if (this.tab is Api.ITabThemeKey key1)
                {
                    return key1.ThemeKeyColor;
                }

                if (this.snapshot is Api.ITabThemeKey key2)
                {
                    return key2.ThemeKeyColor;
                }

                return default;
            }

            set
            {
                if (this.tab is Api.ITabThemeKey key1)
                {
                    key1.ThemeKeyColor = value;
                }
                else if (this.snapshot is Api.ITabThemeKey key2)
                {
                    key2.ThemeKeyColor = value;
                }
            }
        }

        private Brush GetThemeBrush(Func<Api.ITabTheme, Brush> accessor)
        {
            if (this.tab is Api.ITabTheme colored1)
            {
                return accessor(colored1);
            }

            if (this.tab is Api.ITabThemeKey keyColor1 && this.window.App.Settings.GetTabTheme(keyColor1.ThemeKeyColor) is Api.ITabTheme colored2)
            {
                return accessor(colored2);
            }

            if (this.snapshot is Api.ITabTheme colored3)
            {
                return accessor(colored3);
            }

            if (this.snapshot is Api.ITabThemeKey keyColor2 && this.window.App.Settings.GetTabTheme(keyColor2.ThemeKeyColor) is Api.ITabTheme colored4)
            {
                return accessor(colored4);
            }

            return null;
        }

        public Guid Id
        {
            get
            {
                if (this.tab != null)
                {
                    return this.tab.Id;
                }

                if (this.snapshot != null)
                {
                    return this.snapshot.Id;
                }

                return Guid.Empty;
            }
        }

        public string Name
        {
            get
            {
                if (this.tab != null)
                {
                    return this.tab.Name;
                }

                if (this.snapshot != null)
                {
                    return this.snapshot.Name;
                }

                return string.Empty;
            }
        }

        public string Tooltip
        {
            get
            {
                string text = null;

                if (this.tab != null)
                {
                    text = this.tab.Tooltip;
                }

                if (this.snapshot != null)
                {
                    text = this.snapshot.Tooltip;
                }

                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                StringBuilder sb = new StringBuilder(text.Length);
                string[] lines = text.Split("\r\n".ToCharArray(), StringSplitOptions.None);
                const int maxLineLength = 128;
                const int maxLines = 16;

                for (int i = 0; i < maxLines && i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Length > maxLineLength)
                    {
                        line = line.Substring(0, maxLineLength) + "...";
                    }

                    if (i > 0)
                    {
                        sb.Append("\r\n");
                    }

                    sb.Append(line);
                }

                if (lines.Length > maxLines)
                {
                    sb.Append("\r\n...");
                }

                return sb.ToString();
            }
        }

        public Api.ITabSnapshot Snapshot
        {
            get
            {
                return this.tab?.Snapshot ?? this.snapshot;
            }
        }

        public Api.ITab Tab
        {
            get
            {
                if (this.tab == null && this.snapshot != null)
                {
                    Api.ITabSnapshot snapshot = this.snapshot;
                    this.snapshot = null;

                    try
                    {
                        this.restoring = true;
                        this.InitTab(snapshot.Restore(this.window, this.workspace));
                    }
                    finally
                    {
                        this.restoring = false;
                    }
                }

                return this.tab;
            }
        }

        public Api.ActiveState ActiveState
        {
            get
            {
                return this.activeState;
            }

            set
            {
                if (this.SetPropertyValue(ref this.activeState, value))
                {
                    if (this.activeState == Api.ActiveState.Hidden)
                    {
                        this.Tab?.OnHiding();
                    }
                    else
                    {
                        this.Tab?.OnShowing();
                    }
                }
            }
        }

        public ICommand ActivateCommand => new DelegateCommand(() =>
        {
            this.workspace.ActiveTab = this;
        });

        public ICommand CloseCommand => new DelegateCommand(() =>
        {
            if (this.tab == null || this.tab.OnClosing())
            {
                this.workspace.RemoveTab(this);
            }
        });

        public ICommand CloseAllButThisCommand => new DelegateCommand(() =>
        {
            foreach (TabVM tab in this.workspace.Tabs.OfType<TabVM>().ToArray())
            {
                if (tab != this && tab.CloseCommand is ICommand command && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        });

        public void Focus()
        {
            this.Tab?.Focus();
        }

        public bool TakeRestoredTab(Api.ITab tab)
        {
            // There can only be one tab restoring at a time
            if (this.restoring)
            {
                this.InitTab(tab);
                return true;
            }

            return false;
        }
    }
}
