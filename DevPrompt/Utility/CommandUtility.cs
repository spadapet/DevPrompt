using DevPrompt.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevPrompt.Utility
{
    internal static class CommandUtility
    {
        public const string SeparatorName = "Separator";
        private const string NewItemPlaceholder = "{NewItemPlaceholder}";

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
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != CommandUtility.NewItemPlaceholder && dataGrid.SelectedIndex > 0;
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
                return dataGrid.SelectedItem != null && dataGrid.SelectedItem.ToString() != CommandUtility.NewItemPlaceholder && dataGrid.SelectedIndex + 1 < items.Count;
            });
        }

        public static DelegateCommand CreateDeleteCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items, Func<T, bool> readOnlyFunc = null)
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
                if (dataGrid.SelectedItem is T item)
                {
                    return readOnlyFunc == null || !readOnlyFunc(item);
                }

                return false;
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

        public static void UpdateCommands(Dispatcher dispatcher, params DelegateCommand[] commands)
        {
            Action action = () =>
            {
                foreach (DelegateCommand command in commands ?? new DelegateCommand[0])
                {
                    command.UpdateCanExecute();
                }
            };

            dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
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
