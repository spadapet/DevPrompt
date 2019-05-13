
namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model for the tab name dialog
    /// </summary>
    internal class TabNameDialogVM : Api.PropertyNotifier
    {
        private string name;

        public TabNameDialogVM(string name)
        {
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
                this.SetPropertyValue(ref this.name, value ?? string.Empty);
            }
        }
    }
}
