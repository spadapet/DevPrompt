using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI.Settings
{
    internal partial class ToolsSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public Api.DelegateCommand MoveUpCommand { get; }
        public Api.DelegateCommand MoveDownCommand { get; }
        public Api.DelegateCommand DeleteCommand { get; }
        public Api.DelegateCommand ResetCommand { get; }

        public ToolsSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableTools.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableTools);
            this.MoveDownCommand = CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableTools);
            this.DeleteCommand = CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableTools);
            this.ResetCommand = CommandHelpers.CreateResetCommand((s) => s.Tools, this.ViewModel.Settings.ObservableTools, AppSettings.DefaultSettingsFilter.Tools);

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
