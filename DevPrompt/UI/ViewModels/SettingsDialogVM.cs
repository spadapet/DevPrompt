using DevPrompt.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the settings dialog
    /// </summary>
    internal class SettingsDialogVM : Api.PropertyNotifier
    {
        public AppSettings Settings { get; }
        private MainWindow window;
        private SettingsDialog dialog;
        private SettingsTabVM[] tabs;
        private SettingsTabVM activeTab;

        public SettingsDialogVM()
            : this(null, null, null, SettingsTabType.Default)
        {
        }

        public SettingsDialogVM(MainWindow window, SettingsDialog dialog, AppSettings settings, SettingsTabType activeTabType)
        {
            this.window = window;
            this.dialog = dialog;
            this.Settings = settings?.Clone();

            this.tabs = new SettingsTabVM[]
                {
                    new SettingsTabVM(this, SettingsTabType.Consoles),
                    new SettingsTabVM(this, SettingsTabType.Grab),
                    new SettingsTabVM(this, SettingsTabType.Tools),
                    new SettingsTabVM(this, SettingsTabType.Links),
                };

            this.ActiveTabType = activeTabType;
        }

        public IList<SettingsTabVM> Tabs => this.tabs;

        public SettingsTabType ActiveTabType
        {
            get => this.activeTab.TabType;

            set
            {
                foreach (SettingsTabVM tab in this.Tabs)
                {
                    if (tab.TabType == value)
                    {
                        this.ActiveTab = tab;
                        break;
                    }
                }
            }
        }

        public SettingsTabVM ActiveTab
        {
            get => this.activeTab;

            set
            {
                if (this.SetPropertyValue(ref this.activeTab, value ?? this.Tabs.First()))
                {
                    this.OnPropertyChanged(nameof(this.ActiveTabType));
                }
            }
        }

        public ICommand ImportCommand => new Api.DelegateCommand(async () =>
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Import Settings",
                Filter = "XML Files|*.xml",
                DefaultExt = "xml",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this.dialog) == true)
            {
                AppSettings settings = null;
                try
                {
                    settings = await AppSettings.UnsafeLoad(this.window.App, dialog.FileName);
                }
                catch (Exception exception)
                {
                    this.window.ViewModel.SetError(exception);
                }

                if (settings != null)
                {
                    SettingsImportDialog dialog2 = new SettingsImportDialog(settings)
                    {
                        Owner = this.dialog
                    };

                    if (dialog2.ShowDialog() == true)
                    {
                        dialog2.ViewModel.Import(this.Settings);
                    }
                }
            }
        });

        public ICommand ExportCommand => new Api.DelegateCommand(() =>
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Export Settings",
                Filter = "XML Files|*.xml",
                DefaultExt = "xml",
                CheckPathExists = true,
                CheckFileExists = false,
                ValidateNames = true
            };

            if (dialog.ShowDialog(this.dialog) == true)
            {
                this.window.App.SaveSettings(dialog.FileName);
            }
        });
    }
}
