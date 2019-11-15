using DevPrompt.ProcessWorkspace;
using DevPrompt.ProcessWorkspace.Settings;
using DevPrompt.ProcessWorkspace.UI;
using DevPrompt.ProcessWorkspace.UI.ViewModels;
using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for each process tab (handles context menu items, etc)
    /// </summary>
    internal class ProcessTab
        : PropertyNotifier
        , Api.ITab
        , Api.ITabTheme
        , Api.ITabThemeKey
        , IDisposable
    {
        public Guid Id => Guid.Empty;
        public string Name => this.Process.ExpandEnvironmentVariables(this.RawName);
        public string Title => this.Process.Title;
        public string Tooltip => this.Title;
        public string State => this.Process.State;
        public IntPtr Hwnd => this.Process.Hwnd;
        public UIElement ViewElement => null; // No WPF UI, just use ProcessHost HWND
        public Api.ITabSnapshot Snapshot => new ProcessSnapshot(this);
        public Api.IProcess Process { get; }

        private Api.IAppSettings Settings => this.window.App.Settings;
        private readonly Api.IWindow window;
        private readonly Api.IProcessWorkspace workspace;
        private Color themeKeyColor;
        private string name;

        public ProcessTab(Api.IWindow window, Api.IProcessWorkspace workspace, Api.IProcess process)
        {
            this.window = window;
            this.workspace = workspace;
            this.Process = process;
            this.name = string.Empty;

            this.Process.PropertyChanged += this.OnProcessPropertyChanged;
        }

        public void Dispose()
        {
            this.Process.PropertyChanged -= this.OnProcessPropertyChanged;
        }

        private void OnProcessPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(this.Process.Title))
            {
                this.OnPropertyChanged(nameof(this.Tooltip));
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(this.Process.Environment))
            {
                this.OnPropertyChanged(nameof(this.Name));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(this.Process.Hwnd))
            {
                this.OnPropertyChanged(nameof(this.Hwnd));
            }
        }

        public void Focus()
        {
            this.Process.Focus();
        }

        public void OnShowing()
        {
            this.Process.Activate();
        }

        public void OnHiding()
        {
            this.Process.Deactivate();
        }

        public bool OnClosing()
        {
            this.Process.Dispose();

            // When the process goes away, the tab will automatically go away, so don't return true
            return false;
        }

        public string RawName
        {
            get => this.name ?? string.Empty;

            set
            {
                if (this.SetPropertyValue(ref this.name, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.Name));
                }
            }
        }

        public void OnSetTabName()
        {
            this.window.App.Telemetry.TrackEvent("ProcessTab.SetTabName");

            TabNameDialogVM viewModel = new TabNameDialogVM(this.window.App.Settings, this.RawName, this.ThemeKeyColor);
            TabNameDialog dialog = new TabNameDialog(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                this.RawName = viewModel.Name;
                this.ThemeKeyColor = viewModel.ThemeKeyColor;
            }
        }

        public Color ThemeKeyColor
        {
            get => this.themeKeyColor;
            set
            {
                if (this.SetPropertyValue(ref this.themeKeyColor, value))
                {
                    this.OnPropertyChanged(nameof(this.ForegroundSelectedBrush));
                    this.OnPropertyChanged(nameof(this.ForegroundUnselectedBrush));
                    this.OnPropertyChanged(nameof(this.BackgroundSelectedBrush));
                    this.OnPropertyChanged(nameof(this.BackgroundUnselectedBrush));
                }
            }
        }

        Color Api.ITabThemeKey.KeyColor => this.ThemeKeyColor;
        public Brush ForegroundSelectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.ForegroundSelectedBrush : null;
        public Brush ForegroundUnselectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.ForegroundUnselectedBrush : null;
        public Brush BackgroundSelectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.BackgroundSelectedBrush : null;
        public Brush BackgroundUnselectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.BackgroundUnselectedBrush : null;

        public void OnClone()
        {
            this.window.App.Telemetry.TrackEvent("ProcessTab.Clone");
            this.workspace.CloneProcess(this, this.RawName, this.ThemeKeyColor);
        }

        public void OnDetach()
        {
            this.window.App.Telemetry.TrackEvent("ProcessTab.Detach");
            this.Process.Detach();
        }

        public void OnConsoleDefaults()
        {
            this.window.App.Telemetry.TrackEvent("ProcessTab.ConsoleDefaults");
            this.Process.RunCommand(Api.ProcessCommand.DefaultsDialog);
        }

        public void OnConsoleProperties()
        {
            this.window.App.Telemetry.TrackEvent("ProcessTab.ConsoleProperties");
            this.Process.RunCommand(Api.ProcessCommand.PropertiesDialog);
        }

        public IEnumerable<FrameworkElement> ContextMenuItems
        {
            get
            {
                yield return new MenuItem()
                {
                    Header = Resources.Command_SetTabName,
                    InputGestureText = "Ctrl+Shift+T",
                    Command = new DelegateCommand(this.OnSetTabName),
                };

                if (this.CreateTabColorMenu() is MenuItem tabColorMenu)
                {
                    yield return tabColorMenu;
                }

                yield return new Separator();

                yield return new MenuItem()
                {
                    Header = Resources.Command_Clone,
                    InputGestureText = "Ctrl+T",
                    Command = new DelegateCommand(this.OnClone),
                };

                yield return new MenuItem()
                {
                    Header = Resources.Command_ConsoleDefaults,
                    Command = new DelegateCommand(this.OnConsoleDefaults),
                };

                yield return new MenuItem()
                {
                    Header = Resources.Command_ConsoleProperties,
                    Command = new DelegateCommand(this.OnConsoleProperties),
                };

                yield return new Separator();

                yield return new MenuItem()
                {
                    Header = Resources.Command_Detach,
                    InputGestureText = "Ctrl+Shift+F4",
                    Command = new DelegateCommand(this.OnDetach),
                };
            }
        }

        private MenuItem CreateTabColorMenu()
        {
            MenuItem tabColorMenu = null;

            if (this.workspace.ViewElement is ProcessWorkspaceControl control)
            {
                tabColorMenu = new MenuItem()
                {
                    Header = Resources.Command_SetTabColor,
                };

                DataTemplate headerTemplate = (DataTemplate)control.Resources["TabThemeMenuHeaderTemplate"];

                int i = 0;
                foreach (Color keyColor in this.Settings.TabThemeKeys)
                {
                    i++;

                    if (this.Settings.GetTabTheme(keyColor) is Api.ITabTheme theme)
                    {
                        string text = string.Empty;
                        if (i < 10)
                        {
                            text = $"_{i.ToString(CultureInfo.InvariantCulture)}:";
                        }
                        else if (i - 10 < 26)
                        {
                            char ch = (char)('A' + (i - 10));
                            text = $"_{ch.ToString(CultureInfo.InvariantCulture)}:";
                        }

                        tabColorMenu.Items.Add(this.CreateColorItem(text, keyColor, theme, headerTemplate));
                    }
                }
            }

            return tabColorMenu;
        }

        private MenuItem CreateColorItem(string header, Color keyColor, Api.ITabTheme theme, DataTemplate headerTemplate)
        {
            return new MenuItem()
            {
                HeaderTemplate = headerTemplate,
                Header = new TabThemeVM(header, keyColor, theme),
                Command = new DelegateCommand(() => this.ThemeKeyColor = keyColor),
            };
        }
    }
}
