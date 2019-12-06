using System.Windows;

namespace DevPrompt.Update
{
    public sealed partial class App : Application
    {
        public App()
        {
            this.Exit += this.OnAppExit;
        }

        private void OnAppExit(object sender, ExitEventArgs args)
        {
            // TODO: Restart DevPrompt
        }
    }
}
