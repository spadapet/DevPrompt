using DevPrompt.ProcessWorkspace;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for each process tab (handles context menu items, etc)
    /// </summary>
    internal class ProcessTab : Api.PropertyNotifier, Api.ITab, IDisposable
    {
        public Guid Id => Guid.Empty;
        public string Name => this.Process.ExpandEnvironmentVariables(this.RawName);
        public string Title => this.Process.Title;
        public string Tooltip => this.Title;
        public string State => this.Process.State;
        public IntPtr Hwnd => this.Process.Hwnd;
        public UIElement ViewElement => null; // No WPF UI, just use ProcessHost HWND
        public Api.ITabSnapshot Snapshot => new ProcessSnapshot(this);
        public Api.IProcess Process { get; }

        private readonly Api.IWindow window;
        private readonly Api.IProcessWorkspace workspace;
        private string name;

        public ProcessTab(Api.IWindow window, Api.IProcessWorkspace workspace, Api.IProcess process)
        {
            this.window = window;
            this.workspace = workspace;
            this.Process = process;
            this.RawName = string.Empty;

            this.Process.PropertyChanged += this.OnProcessPropertyChanged;
        }

        public void Dispose()
        {
            this.Process.PropertyChanged -= this.OnProcessPropertyChanged;
        }

        private void OnProcessPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(this.Process.Title))
            {
                this.OnPropertyChanged(nameof(this.Tooltip));
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(this.Process.Environment))
            {
                this.OnPropertyChanged(nameof(this.Name));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(this.Process.Hwnd))
            {
                this.OnPropertyChanged(nameof(this.Hwnd));
            }
        }

        public void Focus()
        {
            this.Process.Focus();
        }

        public void OnShowing()
        {
            this.Process.Activate();
        }

        public void OnHiding()
        {
            this.Process.Deactivate();
        }

        public bool OnClosing()
        {
            this.Process.Dispose();

            // When the process goes away, the tab will automatically go away, so don't return true
            return false;
        }

        public string RawName
        {
            get
            {
                return this.name ?? string.Empty;
            }

            set
            {
                if (this.SetPropertyValue(ref this.name, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.Name));
                }
            }
        }

        public ICommand CloneCommand => new Api.DelegateCommand(() =>
        {
            this.workspace.CloneProcess(this, this.RawName);
        });

        public ICommand DetachCommand => new Api.DelegateCommand(() =>
        {
            this.Process.Detach();
        });

        public ICommand DefaultsCommand => new Api.DelegateCommand(() =>
        {
            this.Process.RunCommand(Api.ProcessCommand.DefaultsDialog);
        });

        public ICommand PropertiesCommand => new Api.DelegateCommand(() =>
        {
            this.Process.RunCommand(Api.ProcessCommand.PropertiesDialog);
        });

        public ICommand SetTabNameCommand => new Api.DelegateCommand(() =>
        {
            TabNameDialog dialog = new TabNameDialog(this.RawName)
            {
                Owner = this.window.Window
            };

            if (dialog.ShowDialog() == true)
            {
                this.RawName = dialog.TabName;
            }
        });
    }
}
