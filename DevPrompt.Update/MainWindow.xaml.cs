using System.Threading;
using System.Windows;

namespace DevPrompt.Update
{
    internal sealed partial class MainWindow : Window
    {
        public Worker Worker { get; }
        private CancellationTokenSource cancelSource;

        public MainWindow()
        {
            this.Worker = new Worker();
            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (this.cancelSource == null)
            {
                try
                {
                    using (this.cancelSource = new CancellationTokenSource())
                    {
                        if (await this.Worker.UpdateAsync(this.cancelSource.Token))
                        {
                            this.Close();
                        }
                    }
                }
                finally
                {
                    this.cancelSource = null;
                }
            }
        }

        private void OnCancel(object sender, RoutedEventArgs args)
        {
            if (this.cancelSource is CancellationTokenSource cancelSource)
            {
                cancelSource.Cancel();
            }
            else
            {
                this.Close();
            }
        }
    }
}
