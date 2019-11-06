using DevPrompt.UI.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace DevPrompt.Utility
{
    internal class WorkspaceCommandWrapper : ICommand, IDisposable
    {
        public event EventHandler CanExecuteChanged;

        private readonly IWorkspaceVM workspace;
        private readonly ICommand command;

        public WorkspaceCommandWrapper(IWorkspaceVM workspace, ICommand command)
        {
            this.workspace = workspace;
            this.workspace.PropertyChanged += this.OnWorkspacePropertyChanged;

            this.command = command;
            this.command.CanExecuteChanged += this.OnInnerCanExecuteChanged;
        }

        void IDisposable.Dispose()
        {
            this.workspace.PropertyChanged -= this.OnWorkspacePropertyChanged;
            this.command.CanExecuteChanged -= this.OnInnerCanExecuteChanged;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return this.workspace.ActiveState != Api.ActiveState.Hidden && this.command.CanExecute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            this.command.Execute(parameter);
        }

        private void OnWorkspacePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspaceVM.ActiveState))
            {
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnInnerCanExecuteChanged(object sender, EventArgs args)
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
