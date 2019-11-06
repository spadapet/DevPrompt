using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility.Converters;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace DevPrompt.UI.Settings
{
    internal partial class PluginSettingsControl : UserControl, IDisposable
    {
        public PluginSettingsControlVM ViewModel { get; }

        public PluginSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = new PluginSettingsControlVM(viewModel);
            this.InitializeComponent();
        }

        public void Dispose()
        {
            this.ViewModel.Dispose();
        }

        private void OnHyperlink(object sender, RequestNavigateEventArgs args)
        {
            if (sender is Hyperlink link && link.NavigateUri != null)
            {
                this.ViewModel.Window.ViewModel.RunExternalProcess(link.NavigateUri.ToString());
            }
        }
    }

    internal class PluginStateIconTemplateSelector : DataTemplateSelector
    {
        public DataTemplate InstalledTemplate { get; set; }
        public DataTemplate UpdateAvailableTemplate { get; set; }
        public DataTemplate BusyTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PluginState state)
            {
                if (state.HasFlag(PluginState.Busy))
                {
                    return this.BusyTemplate;
                }

                if (state.HasFlag(PluginState.UpdateAvailable))
                {
                    return this.UpdateAvailableTemplate;
                }

                if (state.HasFlag(PluginState.Installed))
                {
                    return this.InstalledTemplate;
                }
            }

            return this.DefaultTemplate;
        }
    }

    internal class PluginIconTemplateSelector : DataTemplateSelector
    {
        public DataTemplate IconTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ImageSource)
            {
                return this.IconTemplate;
            }

            return this.DefaultTemplate;
        }
    }

    internal class PluginStateToUpdateAvailableVisibility : DelegateConverter
    {
        public PluginStateToUpdateAvailableVisibility()
            : base(PluginStateToUpdateAvailableVisibility.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            if (value is PluginState state)
            {
                if (parameter is bool invert && invert)
                {
                    return !state.HasFlag(PluginState.UpdateAvailable) ? Visibility.Visible : Visibility.Collapsed;
                }

                return state.HasFlag(PluginState.UpdateAvailable) ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new InvalidOperationException();
        }
    }

    internal class PluginStateToInstallVisibility : DelegateConverter
    {
        public PluginStateToInstallVisibility()
            : base(PluginStateToInstallVisibility.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            if (value is PluginState state)
            {
                if (parameter is bool invert && invert)
                {
                    return state.HasFlag(PluginState.Installed) ? Visibility.Visible : Visibility.Collapsed;
                }

                return state.HasFlag(PluginState.Installed) ? Visibility.Collapsed : Visibility.Visible;
            }

            throw new InvalidOperationException();
        }
    }

    internal class PluginStateToUpdateVisibility : DelegateConverter
    {
        public PluginStateToUpdateVisibility()
            : base(PluginStateToUpdateVisibility.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            if (value is PluginState state)
            {
                if (parameter is bool invert && invert)
                {
                    return state.HasFlag(PluginState.UpdateAvailable) ? Visibility.Collapsed : Visibility.Visible;
                }

                return state.HasFlag(PluginState.UpdateAvailable) ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new InvalidOperationException();
        }
    }

    internal class MultiBusyToEnabledConverter : DelegateMultiConverter
    {
        public MultiBusyToEnabledConverter()
            : base(MultiBusyToEnabledConverter.Convert)
        {
        }

        private static object Convert(object[] values, Type targetType, object parameter)
        {
            foreach (object value in values)
            {
                if ((value is bool busy && busy) ||
                    (value is IPluginVM plugin && plugin.State.HasFlag(PluginState.Busy)) ||
                    (value is PluginState state && state.HasFlag(PluginState.Busy)))
                {
                    if (parameter is bool invert && invert)
                    {
                        return true;
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
