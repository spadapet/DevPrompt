using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI.Settings
{
    internal partial class GrabSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public Api.DelegateCommand MoveUpCommand { get; }
        public Api.DelegateCommand MoveDownCommand { get; }
        public Api.DelegateCommand DeleteCommand { get; }
        public Api.DelegateCommand ResetCommand { get; }

        public GrabSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableGrabConsoles.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandUtility.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableGrabConsoles);
            this.MoveDownCommand = CommandUtility.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableGrabConsoles);
            this.DeleteCommand = CommandUtility.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableGrabConsoles);
            this.ResetCommand = CommandUtility.CreateResetCommand((s) => s.GrabConsoles, this.ViewModel.Settings.ObservableGrabConsoles, AppSettings.DefaultSettingsFilter.Grabs);

            this.InitializeComponent();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            CommandUtility.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnSettingsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            CommandUtility.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }
    }
}
