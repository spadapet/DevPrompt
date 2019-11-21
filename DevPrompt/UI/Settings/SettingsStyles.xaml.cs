using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevPrompt.UI.Settings
{
    internal partial class SettingsStyles : ResourceDictionary
    {
        public SettingsStyles()
        {
            this.InitializeComponent();
        }

        private void OnColorComboBoxLoaded(object sender, RoutedEventArgs args)
        {
            if (sender is ComboBox combo)
            {
                combo.Focus();
            }
        }

        private void OnComboCellKeyDown(object sender, KeyEventArgs args)
        {
            if (sender is DataGridCell cell && !cell.IsEditing && args.Key == Key.Space && args.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                cell.IsEditing = true;
            }
        }
    }
}
