using DevPrompt.Settings;
using DevPrompt.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DevPrompt.UI
{
    internal partial class CustomizeGrabConsolesDialog : Window
    {
        private readonly ObservableCollection<GrabConsoleSettings> settings;
        public Api.DelegateCommand MoveUpCommand { get; }
        public Api.DelegateCommand MoveDownCommand { get; }
        public Api.DelegateCommand DeleteCommand { get; }
        public Api.DelegateCommand ResetCommand { get; }

        public CustomizeGrabConsolesDialog(IEnumerable<GrabConsoleSettings> consoles)
        {
            this.settings = new ObservableCollection<GrabConsoleSettings>(consoles.Select(i => i.Clone()));
            this.settings.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.settings);
            this.MoveDownCommand = CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.settings);
            this.DeleteCommand = CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.settings);
            this.ResetCommand = CommandHelpers.CreateResetCommand((s) => s.GrabConsoles, this.settings, AppSettings.DefaultSettingsFilter.Grabs);

            this.InitializeComponent();
        }

        public IList<GrabConsoleSettings> Settings
        {
            get
            {
                return this.settings;
            }
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
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
