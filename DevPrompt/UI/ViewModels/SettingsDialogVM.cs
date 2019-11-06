using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.UI.Settings;
using DevPrompt.Utility;
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
    internal class SettingsDialogVM : PropertyNotifier, IDisposable
    {
        public AppSettings Settings { get; }
        public MainWindow Window { get; }
        public SettingsDialog Dialog { get; }
        public App App => this.Window.App;
        public IList<SettingsTabVM> Tabs => this.tabs;
        public Api.IProgressBar ProgressBar => this.Dialog.progressBar;
        public Api.IInfoBar InfoBar => this.Dialog.infoBar;

        private readonly SettingsTabVM[] tabs;
        private SettingsTabVM activeTab;
        private bool isBusy;

        public SettingsDialogVM()
            : this(null, null, null, SettingsTabType.Default)
        {
        }

        public SettingsDialogVM(MainWindow window, SettingsDialog dialog, AppSettings settings, SettingsTabType activeTabType)
        {
            this.Window = window;
            this.Dialog = dialog;
            this.Settings = settings?.Clone();

            this.tabs = new SettingsTabVM[]
                {
                    new SettingsTabVM(this, SettingsTabType.Consoles),
                    new SettingsTabVM(this, SettingsTabType.Grab),
                    new SettingsTabVM(this, SettingsTabType.Tools),
                    new SettingsTabVM(this, SettingsTabType.Links),
                    new SettingsTabVM(this, SettingsTabType.Plugins),
                    new SettingsTabVM(this, SettingsTabType.Telemetry),
                };

            this.ActiveTabType = activeTabType;
        }

        public void Dispose()
        {
            foreach (SettingsTabVM tab in this.tabs)
            {
                tab.Dispose();
            }
        }

        public bool IsBusy
        {
            get => this.isBusy;
            set => this.SetPropertyValue(ref this.isBusy, value);
        }

        public IDisposable BeginBusy()
        {
            this.IsBusy = true;
            return new DelegateDisposable(() => this.IsBusy = false);
        }

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

        public ICommand ImportCommand => new DelegateCommand(async () =>
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Resources.Settings_ImportDialogTitle,
                Filter = $"{Resources.Settings_XmlFilterName}|*.xml",
                DefaultExt = "xml",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this.Dialog) == true)
            {
                AppSettings settings = null;
                try
                {
                    settings = await AppSettings.UnsafeLoad<AppSettings>(this.App, dialog.FileName);
                }
                catch (Exception exception)
                {
                    this.Window.infoBar.SetError(exception);
                }

                if (settings != null)
                {
                    SettingsImportDialog dialog2 = new SettingsImportDialog(settings)
                    {
                        Owner = this.Dialog
                    };

                    if (dialog2.ShowDialog() == true)
                    {
                        dialog2.ViewModel.Import(this.Settings);
                    }
                }
            }
        });

        public ICommand ExportCommand => new DelegateCommand(() =>
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = Resources.Settings_ExportDialogTitle,
                Filter = $"{Resources.Settings_XmlFilterName}|*.xml",
                DefaultExt = "xml",
                CheckPathExists = true,
                CheckFileExists = false,
                ValidateNames = true
            };

            if (dialog.ShowDialog(this.Dialog) == true)
            {
                this.App.SaveSettings(dialog.FileName);
            }
        });
    }
}
