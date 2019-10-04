using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DevPrompt.UI.Controls
{
    internal partial class TaskProgressBar : UserControl, INotifyPropertyChanged, Api.IProgressBar
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly List<TaskInfo> tasks;

        private class TaskInfo : IDisposable
        {
            public TaskProgressBar Owner { get; set; }
            public Action CancelAction { get; set; }
            public string Text { get; set; }

            public void Dispose()
            {
                this.Owner.Cancel(this);
            }
        }

        public TaskProgressBar()
        {
            this.tasks = new List<TaskInfo>();
            this.InitializeComponent();
        }

        public bool IsLoading => this.tasks.Count > 0;
        public string LoadingText => this.IsLoading ? this.tasks[this.tasks.Count - 1].Text : string.Empty;

        public IDisposable Begin(Action cancelAction, string text)
        {
            Debug.Assert(this.Dispatcher.CheckAccess());

            TaskInfo info = new TaskInfo()
            {
                CancelAction = cancelAction,
                Text = text ?? string.Empty,
            };

            this.PushTask(info);
            return info;
        }

        private void PushTask(TaskInfo info)
        {
            Debug.Assert(this.Dispatcher.CheckAccess());

            info.Owner = this;
            this.tasks.Add(info);
            this.OnPropertyChanged(nameof(this.LoadingText));

            if (this.tasks.Count == 1)
            {
                this.OnPropertyChanged(nameof(this.IsLoading));
            }
        }

        private void ClearTasks()
        {
            if (this.IsLoading)
            {
                this.tasks.Clear();
                this.OnPropertyChanged(nameof(this.LoadingText));
                this.OnPropertyChanged(nameof(this.IsLoading));
            }
        }

        private void Cancel(TaskInfo info)
        {
            Debug.Assert(this.Dispatcher.CheckAccess());

            if (this.tasks.Remove(info))
            {
                this.OnPropertyChanged(nameof(this.LoadingText));

                if (this.tasks.Count == 0)
                {
                    this.OnPropertyChanged(nameof(this.IsLoading));
                }

                info.CancelAction?.Invoke();
            }
        }

        public void Cancel()
        {
            Debug.Assert(this.Dispatcher.CheckAccess());

            foreach (TaskInfo info in this.tasks.ToArray())
            {
                info.Dispose();
            }
        }

        public void TransferTasks(TaskProgressBar otherBar)
        {
            TaskInfo[] tasks = this.tasks.ToArray();
            this.ClearTasks();

            foreach (TaskInfo info in tasks)
            {
                otherBar.PushTask(info);
            }
        }

        private void OnClickCancel(object sender, RoutedEventArgs args)
        {
            this.Cancel();
        }

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
