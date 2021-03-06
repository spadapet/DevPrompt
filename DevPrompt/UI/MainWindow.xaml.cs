﻿using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace DevPrompt.UI
{
    internal sealed partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {
        public MainWindowVM ViewModel { get; }
        public App App { get; }
        public IntPtr Handle => new WindowInteropHelper(this).Handle;

        private bool systemShuttingDown;
        private RestartOnClose restartOnClose;
        private enum RestartOnClose { None, Window, App };

        public MainWindow(App app)
        {
            this.App = app;
            this.ViewModel = new MainWindowVM(this);

            this.InitializeComponent();

            this.keyCtrl1.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl2.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl3.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl4.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl5.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl6.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl7.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl8.Command = this.ViewModel.QuickStartConsoleCommand;
            this.keyCtrl9.Command = this.ViewModel.QuickStartConsoleCommand;

            this.keyCtrl1.CommandParameter = 0;
            this.keyCtrl2.CommandParameter = 1;
            this.keyCtrl3.CommandParameter = 2;
            this.keyCtrl4.CommandParameter = 3;
            this.keyCtrl5.CommandParameter = 4;
            this.keyCtrl6.CommandParameter = 5;
            this.keyCtrl7.CommandParameter = 6;
            this.keyCtrl8.CommandParameter = 7;
            this.keyCtrl9.CommandParameter = 8;

            InputManager.Current.EnterMenuMode += this.OnEnterMenuMode;
            InputManager.Current.LeaveMenuMode += this.OnLeaveMenuMode;
        }

        public void InitWorkspaces(AppSnapshot snapshot)
        {
            this.ViewModel.InitWorkspaces(snapshot);
            this.AddPluginMenuItems(this.mainMenu, Api.MenuType.MenuBar);
            this.AddPluginKeyBindings();
        }

        public UIElement ViewElement
        {
            get => this.viewElementHolder.Child;
            set
            {
                if (this.viewElementHolder.Child != value)
                {
                    this.viewElementHolder.Child = value;
                    this.viewElementHolder.Visibility = (value != null) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private async void OnFileMenuOpened(object sender, RoutedEventArgs args)
        {
            MenuItem menu = (MenuItem)sender;

            this.AddPluginMenuItems(menu, Api.MenuType.File);
            this.UpdateFileMenuConsoles(menu);

            if (await this.ViewModel.UpdateVisualStudioConsolesAsync())
            {
                this.UpdateFileMenuConsoles(menu);
            }
        }

        private void UpdateFileMenuConsoles(MenuItem fileMenu)
        {
            List<ConsoleSettings> consoles = new List<ConsoleSettings>(this.App.Settings.Consoles.Count + this.ViewModel.VisualStudioConsoles.Count);
            consoles.AddRange(this.App.Settings.Consoles);
            consoles.AddRange(this.ViewModel.VisualStudioConsoles);

            MainWindow.UpdateMenu(fileMenu, consoles, (ConsoleSettings settings) =>
            {
                return new MenuItem()
                {
                    Header = settings.MenuName,
                    Command = this.ViewModel.ConsoleCommand,
                    CommandParameter = settings,
                };
            });

            MainWindow.SetFileMenuHotKeys(fileMenu);
        }

        private static void SetFileMenuHotKeys(MenuItem fileMenu)
        {
            char hotKey = '1';
            foreach (object obj in fileMenu.Items)
            {
                if (obj is MenuItem item)
                {
                    if (hotKey <= '9')
                    {
                        item.InputGestureText = $"Ctrl-{hotKey++}";
                    }
                    else
                    {
                        item.InputGestureText = string.Empty;
                    }
                }
                else if (obj is Separator)
                {
                    break;
                }
            }
        }

        private void OnGrabMenuOpened(object sender, RoutedEventArgs args)
        {
            Api.IApp app = this.App;
            List<Api.GrabProcess> grabProcesses = new List<Api.GrabProcess>(app.AppProcesses.GrabProcesses);
            MenuItem menu = (MenuItem)sender;

            MainWindow.UpdateMenu(menu, grabProcesses, (Api.GrabProcess grabProcess) =>
            {
                return new MenuItem()
                {
                    Header = grabProcess.Name,
                    Command = this.ViewModel.GrabConsoleCommand,
                    CommandParameter = grabProcess.Id,
                };
            });

            this.AddPluginMenuItems(menu, Api.MenuType.Grab);
        }

        private void OnToolsMenuOpened(object sender, RoutedEventArgs args)
        {
            MenuItem menu = (MenuItem)sender;

            MainWindow.UpdateMenu(menu, this.App.Settings.Tools, (ToolSettings settings) =>
            {
                if (string.IsNullOrEmpty(settings.Command))
                {
                    return new Separator();
                }

                return new MenuItem()
                {
                    Header = settings.Name,
                    Command = this.ViewModel.ToolCommand,
                    CommandParameter = settings,
                };
            });

            this.AddPluginMenuItems(menu, Api.MenuType.Tools);
        }

        private void OnLinksMenuOpened(object sender, RoutedEventArgs args)
        {
            MenuItem menu = (MenuItem)sender;

            MainWindow.UpdateMenu(menu, this.App.Settings.Links, (LinkSettings settings) =>
            {
                if (string.IsNullOrEmpty(settings.Address))
                {
                    return new Separator();
                }

                return new MenuItem()
                {
                    Header = settings.Name,
                    Command = this.ViewModel.LinkCommand,
                    CommandParameter = settings,
                };
            });

            this.AddPluginMenuItems(menu, Api.MenuType.Links);
        }

        private void OnHelpMenuOpened(object sender, RoutedEventArgs args)
        {
            this.AddPluginMenuItems((MenuItem)sender, Api.MenuType.Help);
        }

        private async void OnUpdateMenuOpened(object sender, RoutedEventArgs args)
        {
            this.infoBar.Clear();
            await this.App.AppUpdate.CheckUpdateVersionAsync();
        }

        /// <summary>
        /// Replaces MenuItems up until the first separator with dynamic MenuItems.
        /// The dynamic MenuItems are generated by the createMenuItem Func
        /// </summary>
        private static void UpdateMenu<T>(MenuItem menu, IList<T> dynamicItems, Func<T, Control> createMenuItem)
        {
            for (int i = 0; i < dynamicItems.Count; i++)
            {
                FrameworkElement item = (FrameworkElement)menu.Items[i];

                if (item.Tag is string str && str == "[End]")
                {
                    // Reached the end separator
                    menu.Items.Insert(i, createMenuItem(dynamicItems[i]));
                }
                else if (!(item.Tag is T itemTag) || !EqualityComparer<T>.Default.Equals(itemTag, dynamicItems[i]))
                {
                    FrameworkElement elem = createMenuItem(dynamicItems[i]);
                    elem.Tag = dynamicItems[i];
                    menu.Items[i] = elem;
                }
            }

            // Delete old extra items
            for (int i = dynamicItems.Count; i < menu.Items.Count; i++)
            {
                FrameworkElement item = (FrameworkElement)menu.Items[i];

                if (item.Tag is string str && str == "[End]")
                {
                    item.Visibility = (dynamicItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
                    break;
                }
                else
                {
                    menu.Items.RemoveAt(i--);
                }
            }
        }

        private void AddPluginMenuItems(ItemsControl menu, Api.MenuType menuType)
        {
            if (!this.App.PluginState.Initialized)
            {
                // Try again later
                return;
            }

            if (menu.Items.OfType<Separator>().Where(s => s.Tag is string name && name == "[Plugins]").FirstOrDefault() is Separator separator)
            {
                separator.Tag = null;
                int index = menu.Items.IndexOf(separator);

                foreach (Api.ICommandProvider provider in this.App.PluginState.CommandProviders)
                {
                    try
                    {
                        foreach (FrameworkElement item in provider.GetMenuItems(menuType, this.ViewModel))
                        {
                            if (item != null)
                            {
                                menu.Items.Insert(index, item);
                            }
                        }
                    }
                    catch
                    {
                        Debug.Fail($"IMenuItemProvider.GetMenuItems failed in plugin class {provider.GetType().FullName}");
                    }
                }

                if (menu.Items.IndexOf(separator) == index)
                {
                    // No plugins added anything
                    separator.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void AddPluginKeyBindings()
        {
            Debug.Assert(this.App.PluginState.Initialized);

            foreach (Api.ICommandProvider provider in this.App.PluginState.CommandProviders)
            {
                try
                {
                    foreach (KeyBinding binding in provider.GetKeyBindings(this.ViewModel))
                    {
                        if (binding != null)
                        {
                            this.InputBindings.Add(binding);
                        }
                    }
                }
                catch
                {
                    Debug.Fail($"IMenuItemProvider.GetKeyBindings failed in plugin class {provider.GetType().FullName}");
                }
            }
        }

        private void OnActivated(object sender, EventArgs args)
        {
            this.App.NativeApp?.Activate();
        }

        private void OnDeactivated(object sender, EventArgs args)
        {
            this.App.NativeApp?.Deactivate();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            this.App.NativeApp?.MainWindowProc(hwnd, msg, wParam, lParam);
            return IntPtr.Zero;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.AddHook(this.WindowProc);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.RemoveHook(this.WindowProc);
            }
        }

        private void OnClosed(object sender, EventArgs args)
        {
            this.App.OnWindowClosed(this.restartOnClose == RestartOnClose.Window, this.restartOnClose == RestartOnClose.App);
        }

        private Task OnClosing()
        {
            this.App.OnWindowClosing(this);

            AppSnapshot snapshot = new AppSnapshot(this.ViewModel, force: this.restartOnClose != RestartOnClose.None);
            return snapshot.Save(this.App);
        }

        private void OnClosing(object sender, CancelEventArgs args)
        {
            if (!this.systemShuttingDown)
            {
                this.OnClosing();
            }
        }

        /// <summary>
        /// Last chance to save snapshot before restart
        /// </summary>
        public void OnSessionEnded()
        {
            if (!this.systemShuttingDown)
            {
                this.systemShuttingDown = true;
                this.OnClosing().Wait();
            }
        }

        public void CloseAndRestart(bool justReopenWindow)
        {
            this.restartOnClose = justReopenWindow ? RestartOnClose.Window : RestartOnClose.App;
            this.Close();
        }

        private void OnKeyEvent(object sender, KeyEventArgs args)
        {
            this.ViewModel.ActiveWorkspace?.Workspace?.OnKeyEvent(args);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            base.OnGotKeyboardFocus(args);

            if (args.NewFocus == this && !InputManager.Current.IsInMenuMode)
            {
                this.ViewModel.FocusActiveWorkspace();
            }
        }

        private void OnEnterMenuMode(object sender, EventArgs args)
        {
            this.Focus();
        }

        private void OnLeaveMenuMode(object sender, EventArgs args)
        {
            this.ViewModel.FocusActiveWorkspace();
        }
    }
}
