namespace Core.Vso
{
    using Microsoft.TeamFoundation.Build.WebApi;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class AdoGitClient
    {
        private VssConnection vssConnection;

        public AdoGitClient(VssConnection vssConnection)
        {
            this.vssConnection = vssConnection;
        }

        public async Task<List<GitRepository>> GetReposAsync(string teamProject)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();
            return await gitHttpClient.GetRepositoriesAsync(teamProject);
        }

        /// <summary>
        /// Get list of commits in batches
        /// </summary>
        public IEnumerable<List<GitCommitRef>> GetCommitsBatch(GitRepository repo, GitQueryCommitsCriteria searchCriteria, int? top = 20000)
        {
            List<GitCommitRef> currentSetOfCommits = new List<GitCommitRef>();
            int? skip = 0;

            string fromDateDisplay = null;

            if (!string.IsNullOrEmpty(searchCriteria.FromDate))
            {
                fromDateDisplay = "from" + searchCriteria.FromDate;
            }

            do
            {
                Console.WriteLine($"Getting list of commits for project {repo.ProjectReference.Name} repo {repo.Name} {fromDateDisplay}");

                GitHttpClient gitHttpClient = this.vssConnection.GetClientAsync<GitHttpClient>().Result;

                currentSetOfCommits = ThrottlingManager.SleepAndRetry(async () => { return await gitHttpClient.GetCommitsBatchAsync(searchCriteria, repo.Id, skip: skip, top: top); }).Result;

                Console.WriteLine($"{currentSetOfCommits.Count} commits found");

                yield return currentSetOfCommits;

                skip = skip + top;
            }

            // Continue to retrieve in batches if search criteria does not contain a list of commit ID's since this is a single operation
            while (currentSetOfCommits.Count > 0 && searchCriteria.Ids == null);
        }

        /// <summary>
        /// Query all pull requests from today until min creation date where min creation date is in the past (inclusive)
        /// </summary>
        public IEnumerable<List<GitPullRequest>> GetPullRequests(GitRepository repo, DateTime toDate, PullRequestStatus status)
        {
            if (toDate > DateTime.UtcNow)
            {
                throw new ArgumentException("minCreationDate must be less than today's date, all in UTC");
            }

            List<GitPullRequest> currentSetOfPullRequests = new List<GitPullRequest>();
            int skip = 0;
            int top = 100;

            GitPullRequestSearchCriteria searchCriteria = new GitPullRequestSearchCriteria
            {
                Status = status,
            };

            do
            {
                Console.WriteLine($"Retrieving {status} pull requests for project {repo.ProjectReference.Name} repo {repo.Name} until {toDate}...");

                // The last pull request is the where we want to check before stopping
                if (status == PullRequestStatus.Completed && currentSetOfPullRequests.Count > 0 && currentSetOfPullRequests.Last().ClosedDate < toDate)
                {
                    Console.WriteLine($"No more pull requests found before {toDate}");
                    break;
                }
                else if (status == PullRequestStatus.Active && currentSetOfPullRequests.Count > 0 && currentSetOfPullRequests.Last().CreationDate < toDate)
                {
                    Console.WriteLine($"No more pull requests found before {toDate}");
                    break;
                }
                else if (status == PullRequestStatus.Abandoned && currentSetOfPullRequests.Count > 0 && currentSetOfPullRequests.Last().ClosedDate < toDate)
                {
                    Console.WriteLine($"No more pull requests found before {toDate}");
                    break;
                }

                // Get pull requests from VSTS
                GitHttpClient gitHttpClient = this.vssConnection.GetClientAsync<GitHttpClient>().Result;
                currentSetOfPullRequests = ThrottlingManager.SleepAndRetry(async () =>
                {
                    try
                    {
                        return await gitHttpClient.GetPullRequestsAsync(repo.Id, searchCriteria, skip: skip, top: top);
                    }
                    catch (VssServiceException ex)
                    {
                        // VSTS service fails to access a repo once in a while. It looks like an exception that we
                        // don't have control over so we'll catch it and move on.
                        // Sample exception: Microsoft.VisualStudio.Services.Common.VssServiceException: TF401019: The Git
                        // repository with name or identifier C50B9441-B35B-4F42-BDA9-9A01386B968F does not exist or you
                        // do not have permissions for the operation you are attempting.
                        if (ex.Message.Contains("TF401019"))
                        {
                            Console.WriteLine($"Warning: Ignore this error due to external VSTS service. {ex}");
                        }
                    }

                    return currentSetOfPullRequests;
                }).Result;

                // VSO returns a chunk each time, filter out the ones that meet the toDate requirements
                if (status == PullRequestStatus.Completed || status == PullRequestStatus.Abandoned)
                {
                    currentSetOfPullRequests = currentSetOfPullRequests.Where(v => v.ClosedDate > toDate).ToList();
                }
                else if (status == PullRequestStatus.Active)
                {
                    currentSetOfPullRequests = currentSetOfPullRequests.Where(v => v.CreationDate > toDate).ToList();
                }

                // Return a batch of requests at a time
                Console.WriteLine($"Retrieved {currentSetOfPullRequests.Count} pull requests");
                yield return currentSetOfPullRequests;

                skip = skip + top;
            }
            while (currentSetOfPullRequests.Count > 0);
        }

        /// <summary>
        /// Gets a list of commits linked to a pull request based on the id of the repo.
        /// </summary>
        /// <param name="repoId">The Git repository id.</param>
        /// <param name="pullRequestId">The pull request id.</param>
        /// <returns>Collection of Pull Request commits.</returns>
        public async Task<List<GitCommitRef>> GetPullRequestCommitsAsync(Guid repoId, int pullRequestId)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();

            List<GitCommitRef> pullRequestCommits = await ThrottlingManager.SleepAndRetry(async () =>
            {
                return await gitHttpClient.GetPullRequestCommitsAsync(repoId, pullRequestId);
            });

            return pullRequestCommits;
        }

        public async Task<List<ResourceRef>> GetPullRequestWorkItemsAsync(Guid repoId, int pullRequestId)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();
            List<ResourceRef> workitems = await ThrottlingManager.SleepAndRetry(async () => await gitHttpClient.GetPullRequestWorkItemRefsAsync(repoId, pullRequestId));

            return workitems;
        }

        /// <summary>
        /// Retrieve items such as files paths in repository
        /// </summary>
        public async Task<List<GitItem>> GetItemsAsync(Guid projectId, Guid repoId, string scopePath, VersionControlRecursionType recursionLevel, GitVersionDescriptor versionDescriptor)
        {
            List<GitItem> items = await ThrottlingManager.SleepAndRetry(async () =>
            {
                GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();
                return await gitHttpClient.GetItemsAsync(projectId, repoId, scopePath: scopePath, recursionLevel: recursionLevel, versionDescriptor: versionDescriptor);
            });

            return items;
        }

        /// <summary>
        /// Obtains a stream with the contents of a file in the repository. Git version descriptor, since we want to get the contents for a specific branch of commit
        /// </summary>
        public async Task<Stream> GetItemTextAsync(Guid projectId, Guid repoId, string filePath, GitVersionDescriptor gitVersionDescriptor)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();

            Stream contents = await ThrottlingManager.SleepAndRetry(async () =>
            {
                return await gitHttpClient.GetItemTextAsync(projectId, repoId, filePath, versionDescriptor: gitVersionDescriptor);
            });

            return contents;
        }

        public async Task<GitBranchStats> GetBranchDetails(Guid repoId, string branchName)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();

            GitBranchStats contents = await ThrottlingManager.SleepAndRetry(async () =>
            {
                return await gitHttpClient.GetBranchAsync(repoId, branchName);
            });

            return contents;
        }

        /// <summary>
        /// Get list of commits in batches
        /// </summary>
        public IEnumerable<List<GitPush>> GetPushes(GitRepository repo, GitPushSearchCriteria searchCriteria, int? top = 5000)
        {
            List<GitPush> currentSet = new List<GitPush>();
            int? skip = 0;

            do
            {
                Console.WriteLine($"Getting pushes for project {repo.ProjectReference.Name} repo {repo.Name} from {searchCriteria.FromDate}");

                GitHttpClient gitHttpClient = this.vssConnection.GetClientAsync<GitHttpClient>().Result;

                currentSet = ThrottlingManager.SleepAndRetry(async () =>
                {
                    return await gitHttpClient.GetPushesAsync(repo.Id, searchCriteria: searchCriteria, skip: skip, top: top);
                }).Result;

                Console.WriteLine($"{currentSet.Count} pushes found");

                yield return currentSet;

                skip = skip + top;
            }
            while (currentSet.Count > 0);
        }

        /// <summary>
        /// Version descriptor specifying that we only want to search within this branch in the repo
        /// </summary>
        public GitVersionDescriptor GetGitVersionDescriptor(GitRepository repo)
        {
            // For example, parse refs/heads/master and get back master
            string branchName = this.GetBranchName(repo.DefaultBranch);

            // Create git version descriptor
            GitVersionDescriptor gitVersionDescriptor = new GitVersionDescriptor
            {
                Version = branchName,
                VersionType = GitVersionType.Branch
            };

            return gitVersionDescriptor;
        }

        /// <summary>
        /// Version descriptor for default branch of buildRepository
        /// </summary>
        public GitVersionDescriptor GetGitVersionDescriptor(BuildRepository repo)
        {
            // For example, parse refs/heads/master and get back master
            string branchName = this.GetBranchName(repo.DefaultBranch);

            // Create git version descriptor
            GitVersionDescriptor gitVersionDescriptor = new GitVersionDescriptor
            {
                Version = branchName,
                VersionType = GitVersionType.Branch
            };

            return gitVersionDescriptor;
        }

        /// <summary>
        /// For example, parse refs/heads/master and get back master
        /// </summary>
        public string GetBranchName(string branchNameRef)
        {
            string branchName = string.Empty;

            if (!string.IsNullOrEmpty(branchNameRef))
            {
                branchName = Regex.Match(branchNameRef, "^refs/heads/(.+)").Groups[1].Value;
            }

            return branchName;
        }

        /// <summary>
        /// Get all the branches for a particular repository
        /// </summary>
        public async Task<List<GitRef>> GetAllBranchesForRepo(Guid repoId)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();

            List<GitRef> branches = await ThrottlingManager.SleepAndRetry(async () =>
            {
                return await gitHttpClient.GetBranchRefsAsync(repoId);
            });

            return branches;
        }

        public async Task<List<GitPullRequestCommentThread>> GetCommentsForPullRequestAsync(Guid repositoryId, int pullRequestId)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();
            return await gitHttpClient.GetThreadsAsync(repositoryId, pullRequestId);
        }

        public async Task<GitCommitChanges> GetGitCommitChangesAsync(string commitId, System.Guid repositoryId)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();
            return await gitHttpClient.GetChangesAsync(commitId, repositoryId);
        }

        /// <summary>
        /// Get Git Repository for a given Id
        /// </summary>
        /// <param name="repoId">Repository Id</param>
        /// <returns>Git Repository</returns>
        public async Task<GitRepository> GetReposAsync(Guid repoId)
        {
            GitHttpClient gitHttpClient = await this.vssConnection.GetClientAsync<GitHttpClient>();
            return await gitHttpClient.GetRepositoryAsync(repoId);
        }
    }
}
