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
    public class AzureDevOpsClient
    {
        private VssConnection connection;

        public async Task<List<Account>> GetAccountsAsync(CancellationToken cancelToken)
        {
            using (VssConnection connection = new VssConnection(new Uri("https://app.vssps.visualstudio.com"), new VssClientCredentials()))
            {
                AccountHttpClient accountsClient = await connection.GetClientAsync<AccountHttpClient>(cancelToken);
                List<Account> accounts = await accountsClient.GetAccountsByMemberAsync(connection.AuthorizedIdentity.Id, cancellationToken:cancelToken);
                return accounts;
            }
        }

        /// <summary>
        /// Return a list of projects from an organization
        /// </summary>
        /// <param name="organizationUri">For example:https://dev.azure.com/microsoft is a valid Uri</param>
        public async Task<IPagedList<TeamProjectReference>> GetProjectsAsync(Uri organizationUri, CancellationToken cancelToken)
        {
            this.InitializeConnection(organizationUri);
            
            ProjectHttpClient projectClient = await this.connection.GetClientAsync<ProjectHttpClient>(cancelToken);

            // Todo: Projects can return more than the default top value, come back and use continuation token
            IPagedList<TeamProjectReference> projects = await projectClient.GetProjects(top: 500);

            return projects;
        }

        public async Task<List<GitPullRequest>> GetPullRequests(Uri organizationUri, string project, GitPullRequestSearchCriteria searchCriteria, CancellationToken cancelToken)
        {
            this.InitializeConnection(organizationUri);
            GitHttpClient gitClient = await this.connection.GetClientAsync<GitHttpClient>(cancelToken);
            List<GitPullRequest> pullRequests = await gitClient.GetPullRequestsByProjectAsync(project, searchCriteria, cancellationToken:cancelToken);
            return pullRequests;
        }

        private void InitializeConnection(Uri organizationUri)
        {
            if (this.connection == null || this.connection.Uri != organizationUri)
            {
                this.connection?.Dispose();
                this.connection = new VssConnection(organizationUri, new VssClientCredentials());
            }
        }
    }
}
