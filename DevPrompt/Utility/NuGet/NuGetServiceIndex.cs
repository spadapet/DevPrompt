using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Utility.NuGet
{
    internal class NuGetServiceIndex : IDisposable
    {
        public const string NuGetOrg = "https://api.nuget.org/v3/index.json";
        public const string PluginSearchTag = "DevPrompt.Plugin";
        public const string PluginSearchHiddenTag = "DevPrompt.Plugin.Hidden";
        private const string SearchQueryService = "SearchQueryService";

        private Api.IHttpClient httpClient;
        private List<NuGetService> services;

        public static async Task<NuGetServiceIndex> Create(Api.IHttpClient httpClient, CancellationToken cancelToken, string url = NuGetServiceIndex.NuGetOrg)
        {
            NuGetServiceIndex nuget = new NuGetServiceIndex(httpClient, cancelToken);
            await nuget.Initialize(url, cancelToken);
            return nuget;
        }

        private NuGetServiceIndex(Api.IHttpClient httpClient, CancellationToken cancelToken)
        {
            this.httpClient = httpClient;
            this.services = new List<NuGetService>();
        }

        public void Dispose()
        {
        }

        private async Task Initialize(string url, CancellationToken cancelToken)
        {
            dynamic servicesRoot = await this.httpClient.GetJsonAsDynamicAsync(url, cancelToken);
            NuGetService[] services = servicesRoot.resources;

            this.services.AddRange(services);
        }

        private string GetServiceUrl(string type)
        {
            foreach (NuGetService service in this.services)
            {
                if (service.type == type)
                {
                    return service.idUrl;
                }
            }

            return string.Empty;
        }

        public async Task<IEnumerable<NuGetSearchResult>> Search(string query, CancellationToken cancelToken)
        {
            string serviceUrl = this.GetServiceUrl(NuGetServiceIndex.SearchQueryService);

            string encodedQuery = WebUtility.UrlEncode(query);
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                dynamic resultsRoot = await this.httpClient.GetJsonAsDynamicAsync($"{serviceUrl}?q={encodedQuery}", cancelToken);
                NuGetSearchResult[] results = resultsRoot.data;
                return results;
            }

            return Array.Empty<NuGetSearchResult>();
        }

        public async Task<NuGetPackageVersionInfo> GetVersionInfo(NuGetSearchResultVersion version, CancellationToken cancelToken)
        {
            return await this.httpClient.GetJsonAsTypeAsync<NuGetPackageVersionInfo>(version.idUrl, cancelToken);
        }
    }
}
