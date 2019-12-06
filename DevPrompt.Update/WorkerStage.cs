using DevPrompt.Update.Utility;
using System.ComponentModel;
using System.Diagnostics;

namespace DevPrompt.Update
{
    internal enum WorkerStageType
    {
        ClosingAppWindows,
        Downloading,
        Installing,
        Done,
    }

    internal sealed class WorkerStage : PropertyNotifier
    {
        public WorkerStageType Stage { get; }
        public bool IsActive => this.worker.CurrentStageType == this.Stage;
        private readonly Worker worker;

        public WorkerStage(Worker worker, WorkerStageType stage)
        {
            this.worker = worker;
            this.worker.PropertyChanged += this.OnWorkerPropertyChanged;

            this.Stage = stage;
        }

        public string Text
        {
            get
            {
                switch (this.Stage)
                {
                    case WorkerStageType.ClosingAppWindows:
                        return Resources.Stage_Closing_Text;

                    case WorkerStageType.Downloading:
                        return Resources.Stage_Downloading_Text;

                    case WorkerStageType.Installing:
                        return Resources.Stage_Installing_Text;

                    case WorkerStageType.Done:
                        return Resources.Stage_Done_Text;

                    default:
                        Debug.Fail($"Missing stage text: {this.Stage}");
                        return string.Empty;
                }
            }
        }

        private void OnWorkerPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            bool all = string.IsNullOrEmpty(args.PropertyName);

            if (all || args.PropertyName == nameof(this.worker.CurrentStageType))
            {
                this.OnPropertyChanged(nameof(this.IsActive));
            }
        }
    }
}
