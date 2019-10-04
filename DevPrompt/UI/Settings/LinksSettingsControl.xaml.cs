using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace DevPrompt.UI.Settings
{
    internal partial class LinksSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public DelegateCommand MoveUpCommand { get; }
        public DelegateCommand MoveDownCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ResetCommand { get; }

        public LinksSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableLinks.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandUtility.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableLinks);
            this.MoveDownCommand = CommandUtility.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableLinks);
            this.DeleteCommand = CommandUtility.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableLinks);
            this.ResetCommand = CommandUtility.CreateResetCommand((s) => s.Links, this.ViewModel.Settings.ObservableLinks, AppSettings.DefaultSettingsFilter.Links);

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
