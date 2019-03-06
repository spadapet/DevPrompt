using DevPrompt.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DevPrompt.Utility
{
    internal static class Helpers
    {
        public const string SeparatorName = "Separator";
        private const string NewItemPlaceholder = "{NewItemPlaceholder}";

        public static void ShowMenuFromAltLetter(ItemsControl mainMenu, int vk)
        {
            foreach (object item in mainMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Header is string header)
                {
                    int i = header.IndexOf('_');
                    if (i >= 0 && i + 1 < header.Length)
                    {
                        char letter = char.ToUpperInvariant(header[i + 1]);
                        if (vk == letter)
                        {
                            menuItem.Focus();
                            menuItem.IsSubmenuOpen = true;

                            Action action = () => Helpers.FocusFirstMenuItem(menuItem);
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
                if (subItem is MenuItem subMenuItem)
                {
                    if (subMenuItem.Focus())
                    {
                        break;
                    }
                }
            }
        }

        public static DelegateCommand CreateMoveUpCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new DelegateCommand(() =>
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
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != Helpers.NewItemPlaceholder && dataGrid.SelectedIndex > 0;
            });
        }

        public static DelegateCommand CreateMoveDownCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new DelegateCommand(() =>
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
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != Helpers.NewItemPlaceholder && dataGrid.SelectedIndex + 1 < items.Count;
            });
        }

        public static DelegateCommand CreateDeleteCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new DelegateCommand(() =>
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
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != Helpers.NewItemPlaceholder;
            });
        }

        public static DelegateCommand CreateResetCommand<T>(Func<AppSettings, IList<T>> newListAccessor, ObservableCollection<T> items, AppSettings.DefaultSettingsFilter filter)
        {
            return new DelegateCommand(async () =>
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

        public static void UpdateCommands(params DelegateCommand[] commands)
        {
            Action action = () =>
            {
                foreach (DelegateCommand command in commands ?? new DelegateCommand[0])
                {
                    command.UpdateCanExecute();
                }
            };

            App.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle);
        }

        public static IEnumerable<string> GetGrabProcesses()
        {
            string names = App.Current.NativeApp.GetGrabProcesses();
            return !string.IsNullOrEmpty(names) ? names.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries) : new string[0];
        }
    }
}
