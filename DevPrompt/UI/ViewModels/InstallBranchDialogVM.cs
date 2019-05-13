using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the dialog to install a VS branch
    /// </summary>
    internal class InstallBranchDialogVM : Api.PropertyNotifier
    {
        private readonly InstallBranchDialog dialog;
        private string name;

        public InstallBranchDialogVM(InstallBranchDialog dialog, string name)
        {
            this.dialog = dialog;
            this.name = name;
        }

        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (this.SetPropertyValue(ref this.name, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.Hyperlink));
                }
            }
        }

        public string Hyperlink
        {
            get
            {
                return $"https://aka.ms/vs/16/int.{this.Name}/vs_Enterprise.exe";
            }
        }

        public ICommand InstallCommand
        {
            get
            {
                return new Api.DelegateCommand(() =>
                {
                    if (this.dialog != null)
                    {
                        this.dialog.DialogResult = true;
                    }
                });
            }
        }
    }
}
