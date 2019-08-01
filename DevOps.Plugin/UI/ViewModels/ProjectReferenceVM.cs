using DevPrompt.Api;
using Microsoft.TeamFoundation.Core.WebApi;

namespace DevOps.UI.ViewModels
{
    internal class ProjectReferenceVM : PropertyNotifier
    {
        public TeamProjectReference Project { get; }

        public ProjectReferenceVM(TeamProjectReference project)
        {
            this.Project = project;
        }

        public string Name => this.Project?.Name ?? "(choose an account first)";
    }
}
