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
    internal partial class CustomizeToolsDialog : Window
    {
        private readonly ObservableCollection<ToolSettings> settings;
        public DelegateCommand MoveUpCommand { get; }
        public DelegateCommand MoveDownCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ResetCommand { get; }

        public CustomizeToolsDialog(IEnumerable<ToolSettings> tools)
        {
            this.settings = new ObservableCollection<ToolSettings>(tools.Select(i => i.Clone()));
            this.settings.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandHelpers.CreateMoveUpCommand(() => this.dataGrid, this.settings);
            this.MoveDownCommand = CommandHelpers.CreateMoveDownCommand(() => this.dataGrid, this.settings);
            this.DeleteCommand = CommandHelpers.CreateDeleteCommand(() => this.dataGrid, this.settings);
            this.ResetCommand = CommandHelpers.CreateResetCommand((s) => s.Tools, this.settings, AppSettings.DefaultSettingsFilter.Tools);

            this.InitializeComponent();
        }

        public IList<ToolSettings> Settings
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
