using DevPrompt.Settings;
using System;
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

        public static Api.Utility.DelegateCommand CreateMoveUpCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new Api.Utility.DelegateCommand(() =>
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

        public static Api.Utility.DelegateCommand CreateMoveDownCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items)
        {
            return new Api.Utility.DelegateCommand(() =>
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

        public static Api.Utility.DelegateCommand CreateDeleteCommand<T>(Func<DataGrid> dataGridAccessor, ObservableCollection<T> items, Func<T, bool> readOnlyFunc = null)
        {
            return new Api.Utility.DelegateCommand(() =>
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

        public static Api.Utility.DelegateCommand CreateResetCommand<T>(Func<AppSettings, ObservableCollection<T>> itemsAccessor, AppSettings settings, AppSettings.DefaultSettingsFilter filter)
        {
            return new Api.Utility.DelegateCommand(() =>
            {
                ObservableCollection<T> list = itemsAccessor(settings);
                list.Clear();

                AppSettings defaultSettings = AppSettings.GetDefaultSettings(filter);
                foreach (T newItem in itemsAccessor(defaultSettings))
                {
                    list.Add(newItem);
                }
            });
        }

        public static Api.Utility.DelegateCommand CreateConsolesResetCommand(AppSettings settings, AppSettings.DefaultSettingsFilter filter)
        {
            return new Api.Utility.DelegateCommand(async () =>
            {
                ObservableCollection<ConsoleSettings> list = settings.ObservableConsoles;
                list.Clear();

                AppSettings defaultSettings = AppSettings.GetDefaultSettings(filter);
                foreach (ConsoleSettings newItem in defaultSettings.ObservableConsoles)
                {
                    list.Add(newItem);
                }

                if (!settings.ShowVisualStudioPrompts)
                {
                    foreach (ConsoleSettings newItem in await AppSettings.GetVisualStudioConsolesAsync())
                    {
                        newItem.RunAtStartup = false;
                        list.Add(newItem);
                    }
                }
            });
        }

        public static void UpdateCommands(Dispatcher dispatcher, params Api.Utility.DelegateCommand[] commands)
        {
            Action action = () =>
            {
                foreach (Api.Utility.DelegateCommand command in commands ?? new Api.Utility.DelegateCommand[0])
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
