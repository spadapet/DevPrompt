using DevPrompt.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevPrompt.Utility
{
    internal static class CommandHelpers
    {
        public const string SeparatorName = "Separator";
        private const string NewItemPlaceholder = "{NewItemPlaceholder}";

        public static void ShowMenuFromAltLetter(ItemsControl mainMenu, int vk)
        {
            foreach (object item in mainMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Header is string header && menuItem.IsEnabled && menuItem.Visibility == Visibility.Visible)
                {
                    int i = header.IndexOf('_');
                    if (i >= 0 && i + 1 < header.Length)
                    {
                        char letter = char.ToUpperInvariant(header[i + 1]);
                        if (vk == letter)
                        {
                            menuItem.Focus();
                            menuItem.IsSubmenuOpen = true;

                            Action action = () => CommandHelpers.FocusFirstMenuItem(menuItem);
                            mainMenu.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action);

                            break;
                        }
                    }
                }
            }
        }

        public static void FocusFirstMenuItem(ItemsControl mainMenu)
        {
            foreach (object subItem in mainMenu.Items)
            {
                if (subItem is MenuItem subMenuItem && subMenuItem.IsEnabled && subMenuItem.Visibility == Visibility.Visible)
                {
                    if (subMenuItem.Focus())
                    {
                        break;
                    }
                }
            }
        }

        public static Api.DelegateCommand CreateMoveUpCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new Api.DelegateCommand(() =>
            {
                DataGrid dataGrid = dataGridAccessor();

                if (dataGrid.SelectedIndex > 0 && dataGrid.SelectedIndex < items.Count)
                {
                    items.Move(dataGrid.SelectedIndex, dataGrid.SelectedIndex - 1);
                }
            },
            () =>
            {
                DataGrid dataGrid = dataGridAccessor();
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != CommandHelpers.NewItemPlaceholder && dataGrid.SelectedIndex > 0;
            });
        }

        public static Api.DelegateCommand CreateMoveDownCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new Api.DelegateCommand(() =>
            {
                DataGrid dataGrid = dataGridAccessor();
                if (dataGrid.SelectedIndex >= 0 && dataGrid.SelectedIndex + 1 < items.Count)
                {
                    items.Move(dataGrid.SelectedIndex, dataGrid.SelectedIndex + 1);
                }
            },
            () =>
            {
                DataGrid dataGrid = dataGridAccessor();
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != CommandHelpers.NewItemPlaceholder && dataGrid.SelectedIndex + 1 < items.Count;
            });
        }

        public static Api.DelegateCommand CreateDeleteCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new Api.DelegateCommand(() =>
            {
                DataGrid dataGrid = dataGridAccessor();
                int i = dataGrid.SelectedIndex;

                if (i >= 0 && i < items.Count)
                {
                    items.RemoveAt(i);
                    if (items.Count > 0)
                    {
                        dataGrid.SelectedIndex = Math.Min(i, items.Count - 1);
                    }
                }
            },
            () =>
            {
                DataGrid dataGrid = dataGridAccessor();
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != CommandHelpers.NewItemPlaceholder;
            });
        }

        public static Api.DelegateCommand CreateResetCommand<T>(Func<AppSettings, IList<T>> newListAccessor, ObservableCollection<T> items, AppSettings.DefaultSettingsFilter filter)
        {
            return new Api.DelegateCommand(async () =>
            {
                AppSettings defaultSettings = await AppSettings.GetDefaultSettings(filter);
                IList<T> newList = newListAccessor(defaultSettings);

                items.Clear();

                foreach (T newItem in newList)
                {
                    items.Add(newItem);
                }
            });
        }

        public static void UpdateCommands(Dispatcher dispatcher, params Api.DelegateCommand[] commands)
        {
            Action action = () =>
            {
                foreach (Api.DelegateCommand command in commands ?? new Api.DelegateCommand[0])
                {
                    command.UpdateCanExecute();
                }
            };

            dispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle);
        }

        public static void SafeExecute(this ICommand command, object parameter = null)
        {
            if (command != null && command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }
}
