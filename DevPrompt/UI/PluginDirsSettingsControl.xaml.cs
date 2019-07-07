using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI
{
    internal partial class PluginDirsSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public Api.DelegateCommand MoveUpCommand { get; }
        public Api.DelegateCommand MoveDownCommand { get; }
        public Api.DelegateCommand DeleteCommand { get; }
        public Api.DelegateCommand ResetCommand { get; }

        public PluginDirsSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableUserPluginDirectories.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableUserPluginDirectories);
            this.MoveDownCommand = CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableUserPluginDirectories);
            this.DeleteCommand = CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableUserPluginDirectories, o => o.ReadOnly);
            this.ResetCommand = CommandHelpers.CreateResetCommand((s) => s.UserPluginDirectories, this.ViewModel.Settings.ObservableUserPluginDirectories, AppSettings.DefaultSettingsFilter.PluginDirs);

            this.InitializeComponent();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            CommandHelpers.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnSettingsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            CommandHelpers.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs args)
        {
            if (args.Row.Item is PluginDirectorySettings setting && setting.ReadOnly)
            {
                args.Cancel = true;
            }
        }
    }
}
