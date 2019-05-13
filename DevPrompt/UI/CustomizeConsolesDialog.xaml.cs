using DevPrompt.Settings;
using DevPrompt.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace DevPrompt.UI
{
    internal partial class CustomizeConsolesDialog : Window
    {
        private readonly ObservableCollection<ConsoleSettings> settings;
        public Api.DelegateCommand MoveUpCommand { get; }
        public Api.DelegateCommand MoveDownCommand { get; }
        public Api.DelegateCommand DeleteCommand { get; }
        public Api.DelegateCommand ResetCommand { get; }

        public CustomizeConsolesDialog(IEnumerable<ConsoleSettings> consoles)
        {
            this.settings = new ObservableCollection<ConsoleSettings>(consoles.Select(i => i.Clone()));
            this.settings.CollectionChanged += this.OnConsoleSettingsChanged;

            this.MoveUpCommand = CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.settings);
            this.MoveDownCommand = CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.settings);
            this.DeleteCommand = CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.settings);
            this.ResetCommand = CommandHelpers.CreateResetCommand((s) => s.Consoles, this.settings,
                AppSettings.DefaultSettingsFilter.DevPrompts | AppSettings.DefaultSettingsFilter.RawPrompts);

            this.InitializeComponent();
        }

        public IList<ConsoleSettings> Settings
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

        private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs args)
        {
            CommandHelpers.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnConsoleSettingsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            CommandHelpers.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }
    }
}
