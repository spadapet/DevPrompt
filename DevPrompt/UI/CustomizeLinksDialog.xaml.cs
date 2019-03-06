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
    internal partial class CustomizeLinksDialog : Window
    {
        private readonly ObservableCollection<LinkSettings> settings;
        public DelegateCommand MoveUpCommand { get; }
        public DelegateCommand MoveDownCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ResetCommand { get; }

        public CustomizeLinksDialog(IEnumerable<LinkSettings> links)
        {
            this.settings = new ObservableCollection<LinkSettings>(links.Select(i => i.Clone()));
            this.settings.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = Helpers.CreateMoveUpCommand(() => this.dataGrid, this.settings);
            this.MoveDownCommand = Helpers.CreateMoveDownCommand(() => this.dataGrid, this.settings);
            this.DeleteCommand = Helpers.CreateDeleteCommand(() => this.dataGrid, this.settings);
            this.ResetCommand = Helpers.CreateResetCommand((s) => s.Links, this.settings, AppSettings.DefaultSettingsFilter.Links);

            this.InitializeComponent();
        }

        public IList<LinkSettings> Settings
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
            Helpers.UpdateCommands(this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnSettingsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Helpers.UpdateCommands(this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }
    }
}
