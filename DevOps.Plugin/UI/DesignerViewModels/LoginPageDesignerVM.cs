using System.Windows.Input;

namespace DevOps.UI.DesignerViewModels
{
    internal class LoginPageDesignerVM
    {
        /// <summary>
        /// Sample data for the XAML designer
        /// </summary>
        public LoginPageDesignerVM()
        {
        }

        public string OrganizationName { get; set; } = "OrgName";
        public string ProjectName { get; set; } = "ProjectName";
        public string PersonalAccessToken { get; set; } = "123abcdef";
        public ICommand OkCommand => null;
    }
}
