﻿using DevPrompt.Settings;
using System.Windows;

namespace DevPrompt.UI
{
    internal partial class SettingsImportDialog : Window
    {
        public SettingsImportDialogVM ViewModel { get; }

        public SettingsImportDialog(AppSettings settings)
        {
            this.ViewModel = new SettingsImportDialogVM(settings);

            this.InitializeComponent();
        }

        private void OnClickOk(object sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }
    }
}