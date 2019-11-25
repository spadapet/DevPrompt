using System;
using System.Windows.Threading;
using DevPrompt.ProcessWorkspace.Utility;

namespace DevPrompt.Utility
{
    internal class UpdateState : PropertyNotifier, IDisposable
    {
        private App app;
        private DispatcherTimer timer;
        private static TimeSpan InitialInterval = TimeSpan.FromSeconds(10);
        private static TimeSpan Interval = TimeSpan.FromHours(23);

        public UpdateState(App app)
        {
            this.app = app;
        }

        public void Dispose()
        {
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer = null;
            }
        }

        public void Start()
        {
            if (this.timer == null)
            {
                this.timer = new DispatcherTimer(UpdateState.InitialInterval, DispatcherPriority.ApplicationIdle, this.OnTimer, this.app.Dispatcher);
            }
        }

        private void OnTimer(object sender, EventArgs args)
        {
            if (this.timer != null && this.timer.Interval != UpdateState.Interval)
            {
                this.timer.Interval = UpdateState.Interval;
            }

            this.CheckUpdate();
        }

        private void CheckUpdate()
        {
        }
    }
}
