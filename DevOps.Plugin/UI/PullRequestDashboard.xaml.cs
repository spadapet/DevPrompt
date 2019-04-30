using DevOps.ViewModels;
using System.Windows.Controls;

namespace DevOps.UI
{
    /// <summary>
    /// Interaction logic for PullRequestDashboard.xaml
    /// </summary>
    internal partial class PullRequestDashboard : UserControl
    {
        public PullRequestDashboardVM ViewModel { get; }

        public PullRequestDashboard(PullRequestDashboardVM viewModel)
        {
            this.ViewModel = viewModel;
            this.InitializeComponent();
        }
    }
}
