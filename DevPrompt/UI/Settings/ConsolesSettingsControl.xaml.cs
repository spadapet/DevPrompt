using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI.Settings
{
    internal partial class ConsolesSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public DelegateCommand MoveUpCommand { get; }
        public DelegateCommand MoveDownCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ResetCommand { get; }

        public ConsolesSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableConsoles.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandUtility.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableConsoles);
            this.MoveDownCommand = CommandUtility.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableConsoles);
            this.DeleteCommand = CommandUtility.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableConsoles);
            this.ResetCommand = CommandUtility.CreateConsolesResetCommand(this.ViewModel.Settings, AppSettings.DefaultSettingsFilter.InternalDevPrompts | AppSettings.DefaultSettingsFilter.RawPrompts);

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
