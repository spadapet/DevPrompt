using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI
{
    internal partial class GrabSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public Api.DelegateCommand MoveUpCommand => CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableGrabConsoles);
        public Api.DelegateCommand MoveDownCommand => CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableGrabConsoles);
        public Api.DelegateCommand DeleteCommand => CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableGrabConsoles);
        public Api.DelegateCommand ResetCommand => CommandHelpers.CreateResetCommand((s) => s.GrabConsoles, this.ViewModel.Settings.ObservableGrabConsoles, AppSettings.DefaultSettingsFilter.Grabs);

        public GrabSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableGrabConsoles.CollectionChanged += this.OnSettingsChanged;

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
