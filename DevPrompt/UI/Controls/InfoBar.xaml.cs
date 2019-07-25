using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DevPrompt.UI.Controls
{
    internal partial class InfoBar : UserControl, INotifyPropertyChanged, Api.IInfoBar
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool HasText => !string.IsNullOrEmpty(this.Text);
        public bool HasDetails => !string.IsNullOrEmpty(this.Details);
        private string text;
        private string details;

        public InfoBar()
        {
            this.text = string.Empty;
            this.details = string.Empty;
            this.InitializeComponent();
        }

        public void SetError(Exception exception, string text = null)
        {
            // If the user canceled a task, they don't need to know about it
            if (!(exception is TaskCanceledException))
            {
                this.Text = string.IsNullOrEmpty(text) ? (exception?.Message ?? string.Empty) : text;
                this.Details = exception?.ToString() ?? string.Empty;
            }
        }

        public string Text
        {
            get => this.text;

            set
            {
                if (!string.Equals(this.text, value ?? string.Empty, StringComparison.Ordinal))
                {
                    this.text = value ?? string.Empty;
                    this.OnPropertyChanged(nameof(this.Text));
                    this.OnPropertyChanged(nameof(this.HasText));
                }
            }
        }

        public string Details
        {
            get => this.details;

            set
            {
                if (!string.Equals(this.details, value ?? string.Empty, StringComparison.Ordinal))
                {
                    this.details = value ?? string.Empty;
                    this.OnPropertyChanged(nameof(this.Details));
                    this.OnPropertyChanged(nameof(this.HasDetails));
                }
            }
        }

        public void Clear()
        {
            this.SetError(null);
        }

        private void OnClickBar(object sender, RoutedEventArgs args)
        {
            this.Clear();
        }

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
