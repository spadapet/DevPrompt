using DevPrompt.Settings;
using DevPrompt.UI.Plugins;
using DevPrompt.Utility.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the plugins dialog
    /// </summary>
    internal class PluginsDialogVM : Api.PropertyNotifier, IDisposable
    {
        public AppSettings Settings { get; }
        private MainWindow window;
        private PluginsDialog dialog;
        private PluginsTabVM[] tabs;
        private PluginsTabVM activeTab;
        private CancellationTokenSource cancelSource;
        private HttpClient httpClient;

        public PluginsDialogVM()
            : this(null, null, null, PluginsTabType.Default)
        {
        }

        public PluginsDialogVM(MainWindow window, PluginsDialog dialog, AppSettings settings, PluginsTabType activeTabType)
        {
            this.window = window;
            this.dialog = dialog;
            this.cancelSource = new CancellationTokenSource();
            this.httpClient = new HttpClient();
            this.Settings = settings?.Clone();

            this.tabs = new PluginsTabVM[]
                {
                    new PluginsTabVM(this, PluginsTabType.PluginInstalled),
                    new PluginsTabVM(this, PluginsTabType.PluginAvailable),
                    new PluginsTabVM(this, PluginsTabType.PluginUpdates),
                };

            this.ActiveTabType = activeTabType;

            this.Initialize();
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
            this.cancelSource.Dispose();

            this.httpClient.CancelPendingRequests();
            this.httpClient.Dispose();
        }

        public /*async*/ void Initialize()
        {
            try
            {
                //string servicesJsonText = await this.httpClient.GetStringAsync("https://api.nuget.org/v3/index.json");
                //Api.IJsonValue servicesRoot = JsonParser.Parse(servicesJsonText);
                //foreach (Api.IJsonValue service in servicesRoot["resources"])
                //{
                //}
            }
            catch
            {
            }
        }

        public IList<PluginsTabVM> Tabs => this.tabs;

        public PluginsTabType ActiveTabType
        {
            get => this.activeTab.TabType;

            set
            {
                foreach (PluginsTabVM tab in this.Tabs)
                {
                    if (tab.TabType == value)
                    {
                        this.ActiveTab = tab;
                        break;
                    }
                }
            }
        }

        public PluginsTabVM ActiveTab
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
    }
}
