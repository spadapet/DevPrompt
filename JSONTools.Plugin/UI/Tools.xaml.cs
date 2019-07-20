using JSONTools.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace JSONTools.UI
{
    internal partial class Tools: UserControl
    {
        public JSONToolsTab Tab { get; }
        public ToolsVM ViewModel { get; }

        public Tools(JSONToolsTab tab)
        {
            this.Tab = tab;
            this.ViewModel = new ToolsVM();
            this.InitializeComponent();
        }

        private void OnValidate(object sender, RoutedEventArgs e)
        {
            this.ViewModel.OnValidate();
        }

        private void OnPrettify(object sender, RoutedEventArgs e)
        {
            this.ViewModel.OnPrettify();
        }

        private void OnStringify(object sender, RoutedEventArgs e)
        {
            this.ViewModel.OnStringify();
        }
    }
}
