using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI
{
    internal partial class ConsolesSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public Api.DelegateCommand MoveUpCommand => CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableConsoles);
        public Api.DelegateCommand MoveDownCommand => CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableConsoles);
        public Api.DelegateCommand DeleteCommand => CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableConsoles);
        public Api.DelegateCommand ResetCommand => CommandHelpers.CreateResetCommand((s) => s.Consoles, this.ViewModel.Settings.ObservableConsoles, AppSettings.DefaultSettingsFilter.DevPrompts | AppSettings.DefaultSettingsFilter.RawPrompts);

        public ConsolesSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableConsoles.CollectionChanged += this.OnSettingsChanged;

            this.InitializeComponent();
        }

        private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs args)
        {
            CommandHelpers.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnSettingsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            CommandHelpers.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }
    }
}
