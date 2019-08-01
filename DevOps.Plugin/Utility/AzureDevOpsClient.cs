using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Account.Client;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DevOps.Utility
{
    public class AzureDevOpsClient : IDisposable
    {
        public static AuthenticationResult AuthResult { get; private set; }

        private readonly VssConnection connection;

        public AzureDevOpsClient(Uri accountUri, AzureDevOpsUserContext userContext)  
        {
            this.connection = new VssConnection(accountUri, userContext.VssAadCredential);
        }

        public static async Task<AzureDevOpsUserContext> GetUserContext(CancellationToken cancelToken = default)
        {
            const string aadAuthority = "https://login.windows.net/microsoft.com";
            const string aadResource = "499b84ac-1321-427f-aa17-267ca6975798";
            const string aadClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
            AuthenticationContext authCtx = new AuthenticationContext(aadAuthority);
            UserCredential userCredential = new UserCredential();

            AzureDevOpsUserContext result = new AzureDevOpsUserContext();
            result.AuthenticationResult = await authCtx.AcquireTokenAsync(aadResource, aadClientId, userCredential);
            result.VssAadCredential = new VssAadCredential(new VssAadToken(authCtx, userCredential));

            using (VssConnection connection = new VssConnection(new Uri("https://app.vssps.visualstudio.com"), result.VssAadCredential))
            {
                AccountHttpClient accountsClient = await connection.GetClientAsync<AccountHttpClient>(cancelToken);
                result.Accounts = await accountsClient.GetAccountsByMemberAsync(connection.AuthorizedIdentity.Id, cancellationToken: cancelToken);
            }

            return result;
        }

        public async Task<IPagedList<TeamProjectReference>> GetProjectsAsync(CancellationToken cancelToken = default)
        {
            ProjectHttpClient projectClient = await this.connection.GetClientAsync<ProjectHttpClient>(cancelToken);
            // Todo: Projects can return more than the default top value, come back and use continuation token
            IPagedList<TeamProjectReference> projects = await projectClient.GetProjects(top: 500);
            return projects;
        }

        public async Task<Tuple<Uri, List<GitPullRequest>>> GetPullRequests(string project, GitPullRequestSearchCriteria searchCriteria, CancellationToken cancelToken = default)
        {
            GitHttpClient gitClient = await this.connection.GetClientAsync<GitHttpClient>(cancelToken);
            List<GitPullRequest> pullRequests = await gitClient.GetPullRequestsByProjectAsync(project, searchCriteria, cancellationToken: cancelToken);
            return new Tuple<Uri, List<GitPullRequest>>(gitClient.BaseAddress, pullRequests);
        }

        public void Dispose()
        {
            this.connection?.Dispose();
        }
    }
}
