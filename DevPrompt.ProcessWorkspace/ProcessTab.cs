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
        private Api.ITelemetry Telemetry => this.window.App.Telemetry;
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
            this.Telemetry.TrackEvent("ProcessTab.SetTabName");

            TabNameDialogVM viewModel = new TabNameDialogVM(this.window.App.Settings, this.RawName, this.ThemeKeyColor);
            TabNameDialog dialog = new TabNameDialog(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                this.RawName = viewModel.Name;
                this.UserSetThemeKeyColor(viewModel.ThemeKeyColor, "TabNameDialog");
            }
        }

        public void UserSetThemeKeyColor(Color color, string source)
        {
            if (this.ThemeKeyColor != color)
            {
                this.ThemeKeyColor = color;
                this.Telemetry.TrackEvent("ProcessTab.SetTabColor", new Dictionary<string, object>()
                {
                    { "Source", source },
                });
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

        public Brush ForegroundSelectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.ForegroundSelectedBrush : null;
        public Brush ForegroundUnselectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.ForegroundUnselectedBrush : null;
        public Brush BackgroundSelectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.BackgroundSelectedBrush : null;
        public Brush BackgroundUnselectedBrush => (this.Settings.GetTabTheme(this.ThemeKeyColor) is Api.ITabTheme theme) ? theme.BackgroundUnselectedBrush : null;

        public void OnClone()
        {
            this.Telemetry.TrackEvent("ProcessTab.Clone");
            this.workspace.CloneProcess(this, this.RawName, this.ThemeKeyColor);
        }

        public void OnDetach()
        {
            this.Telemetry.TrackEvent("ProcessTab.Detach");
            this.Process.Detach();
        }

        public void OnConsoleDefaults()
        {
            this.Telemetry.TrackEvent("ProcessTab.ConsoleDefaults");
            this.Process.RunCommand(Api.ProcessCommand.DefaultsDialog);
        }

        public void OnConsoleProperties()
        {
            this.Telemetry.TrackEvent("ProcessTab.ConsoleProperties");
            this.Process.RunCommand(Api.ProcessCommand.PropertiesDialog);
        }

        public void OnEditColors()
        {
            this.window.ShowSettingsDialog(Api.SettingsTabType.Colors);
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
                Separator separator = new Separator();
                MenuItem editColorsItem = new MenuItem()
                {
                    Header = Resources.Command_EditColors,
                    Command = new DelegateCommand(this.OnEditColors),
                };

                tabColorMenu.Items.Add(separator);
                tabColorMenu.Items.Add(editColorsItem);

                tabColorMenu.SubmenuOpened += (s, a) =>
                {
                    this.UpdateTabColorMenu(tabColorMenu, headerTemplate, separator, editColorsItem);
                };
            }

            return tabColorMenu;
        }

        private void UpdateTabColorMenu(MenuItem menu, DataTemplate colorHeaderTemplate, Separator separator, MenuItem editColorsItem)
        {
            int oldCount = menu.Items.IndexOf(separator);
            IReadOnlyList<Api.ITabThemeKey> themeKeys = this.Settings.TabThemeKeys;
            bool needsUpdate = (oldCount != themeKeys.Count);

            for (int i = 0; !needsUpdate && i < oldCount; i++)
            {
                if (!(menu.Items[i] is MenuItem item) || !(item.Header is TabThemeVM itemVM) || itemVM.ThemeKeyColor != themeKeys[i].ThemeKeyColor)
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                menu.Items.Clear();

                int count = 0;
                foreach (Api.ITabThemeKey themeKey in themeKeys)
                {
                    count++;

                    if (this.Settings.GetTabTheme(themeKey.ThemeKeyColor) is Api.ITabTheme theme)
                    {
                        string text = string.Empty;
                        if (count < 10)
                        {
                            text = $"_{count.ToString(CultureInfo.InvariantCulture)}:";
                        }
                        else if (count - 10 < 26)
                        {
                            char ch = (char)('A' + (count - 10));
                            text = $"_{ch.ToString(CultureInfo.InvariantCulture)}:";
                        }

                        menu.Items.Add(new MenuItem()
                        {
                            HeaderTemplate = colorHeaderTemplate,
                            Header = new TabThemeVM(text, themeKey.ThemeKeyColor, theme),
                            Command = new DelegateCommand(() => this.UserSetThemeKeyColor(themeKey.ThemeKeyColor, "ContextMenu")),
                        });
                    }
                }

                menu.Items.Add(separator);
                menu.Items.Add(editColorsItem);
            }
        }
    }
}
