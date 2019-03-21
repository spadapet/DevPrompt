﻿using DevPrompt.Settings;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DevPrompt.UI
{
    internal partial class MainWindow : Window
    {
        public MainWindowVM ViewModel { get; }
        public AppSettings AppSettings { get; }
        public ProcessHostWindow ProcessHostWindow => this.processHostWindow;
        private bool everLoaded;

        public MainWindow(AppSettings settings)
        {
            this.AppSettings = settings;
            this.ViewModel = new MainWindowVM(this);

            this.InitializeComponent();
        }

        private void OnFileMenuOpened(object sender, RoutedEventArgs args)
        {
            MainWindow.UpdateMenu((MenuItem)sender, this.AppSettings.Consoles, (ConsoleSettings settings) =>
            {
                return new MenuItem()
                {
                    Header = settings.MenuName,
                    Command = this.ViewModel.ConsoleCommand,
                    CommandParameter = settings,
                };
            });
        }

        private void OnGrabMenuOpened(object sender, RoutedEventArgs args)
        {
            List<string> names = new List<string>(CommandHelpers.GetGrabProcesses());

            MainWindow.UpdateMenu((MenuItem)sender, names, (string name) =>
            {
                int id = 0;
                if (name.StartsWith("[", StringComparison.Ordinal))
                {
                    int end = name.IndexOf(']', 1);
                    if (end != -1 && int.TryParse(name.Substring(1, end - 1), out int tempId))
                    {
                        id = tempId;
                    }
                }

                return new MenuItem()
                {
                    Header = name,
                    Command = this.ViewModel.GrabConsoleCommand,
                    CommandParameter = id
                };
            });
        }

        private async void OnVsMenuOpened(object sender, RoutedEventArgs args)
        {
            List<VisualStudioSetup.Instance> instances = new List<VisualStudioSetup.Instance>(await VisualStudioSetup.GetInstancesAsync());

            MainWindow.UpdateMenu((MenuItem)sender, instances, (VisualStudioSetup.Instance instance) =>
            {
                return new MenuItem()
                {
                    Header = instance.DisplayName,
                    Command = this.ViewModel.VisualStudioCommand,
                    CommandParameter = instance,
                };
            });
        }

        private void OnToolsMenuOpened(object sender, RoutedEventArgs args)
        {
            MainWindow.UpdateMenu((MenuItem)sender, this.AppSettings.Tools, (ToolSettings settings) =>
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
        }

        private void OnLinksMenuOpened(object sender, RoutedEventArgs args)
        {
            MainWindow.UpdateMenu((MenuItem)sender, this.AppSettings.Links, (LinkSettings settings) =>
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
        }

        /// <summary>
        /// Replaces MenuItems up until the first separator with dynamic MenuItems.
        /// The dynamic MenuItems are generated by the createMenuItem Func
        /// </summary>
        private static void UpdateMenu<T>(MenuItem menu, IList<T> dynamicItems, Func<T, Control> createMenuItem) where T : class
        {
            for (int i = 0; i < dynamicItems.Count; i++)
            {
                FrameworkElement item = (FrameworkElement)menu.Items[i];

                if (item.Tag is string str && str == "[End]")
                {
                    // Reached the end separator
                    menu.Items.Insert(i, createMenuItem(dynamicItems[i]));
                }
                else if (!object.Equals(item.Tag, dynamicItems[i]))
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

        private void OnActivated(object sender, EventArgs args)
        {
            App.Current.NativeApp.Activate();
            this.ProcessHostWindow.OnActivated();
        }

        private void OnDeactivated(object sender, EventArgs args)
        {
            App.Current.NativeApp.Deactivate();
            this.ProcessHostWindow.OnDeactivated();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //System.Diagnostics.Debug.WriteLine($"HWND:{hwnd}, MSG:{msg}, WP:{wParam}, LP:{lParam}");
            return IntPtr.Zero;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (!this.everLoaded)
            {
                this.everLoaded = true;

                if (PresentationSource.FromVisual(this) is HwndSource source)
                {
                    source.AddHook(this.WindowProc);
                }

                Action action = () => this.ViewModel.RunStartupConsoles();
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action);
            }
        }

        private async void OnClosing(object sender, CancelEventArgs args)
        {
            AppSnapshot snapshot = new AppSnapshot(this.ViewModel);
            await snapshot.Save();
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            base.OnGotKeyboardFocus(args);

            if (args.NewFocus == this)
            {
                this.ViewModel.FocusActiveProcess();
            }
        }

        public void OnAltLetter(int vk)
        {
            CommandHelpers.ShowMenuFromAltLetter(this.MainMenu, vk);
        }

        public void OnAlt()
        {
            if (!this.MainMenu.IsKeyboardFocusWithin)
            {
                CommandHelpers.FocusFirstMenuItem(this.MainMenu);
            }
        }

        private void OnTabButtonMouseDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Middle &&
                sender is Button button &&
                button.DataContext is ProcessVM process)
            {
                process.CloseCommand.Execute(null);
            }
        }

        private void OnTabContextMenuOpened(object sender, RoutedEventArgs args)
        {
            if (sender is ContextMenu menu)
            {
                CommandHelpers.FocusFirstMenuItem(menu);
            }
        }
    }
}
