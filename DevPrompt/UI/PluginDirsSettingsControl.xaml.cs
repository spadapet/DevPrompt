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
            this.ViewModel.Settings.ObservablePluginDirectories.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservablePluginDirectories);
            this.MoveDownCommand = CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservablePluginDirectories);
            this.DeleteCommand = CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservablePluginDirectories);
            this.ResetCommand = CommandHelpers.CreateResetCommand((s) => s.PluginDirectories, this.ViewModel.Settings.ObservablePluginDirectories, AppSettings.DefaultSettingsFilter.PluginDirs);

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
    }
}
