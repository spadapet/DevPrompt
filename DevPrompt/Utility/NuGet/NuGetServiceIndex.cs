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
        public const string PluginSearchQuery = "DevPrompt.Plugin";
        private const string SearchQueryService = "SearchQueryService";

        private Api.IHttpClient httpClient;
        private CancellationTokenSource cancelSource;
        private List<NuGetService> services;

        public static async Task<NuGetServiceIndex> Create(Api.IHttpClient httpClient, string url = NuGetServiceIndex.NuGetOrg)
        {
            NuGetServiceIndex nuget = new NuGetServiceIndex(httpClient);
            await nuget.Initialize(url);
            return nuget;
        }

        private NuGetServiceIndex(Api.IHttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.cancelSource = new CancellationTokenSource();
            this.services = new List<NuGetService>();
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
            this.cancelSource.Dispose();
        }

        private async Task Initialize(string url)
        {
            dynamic servicesRoot = await this.httpClient.GetJsonAsDynamicAsync(url, this.cancelSource.Token);
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

        public async Task<IEnumerable<NuGetSearchResult>> Search(string query)
        {
            string serviceUrl = this.GetServiceUrl(NuGetServiceIndex.SearchQueryService);

            string encodedQuery = WebUtility.UrlEncode(query);
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                dynamic resultsRoot = await this.httpClient.GetJsonAsDynamicAsync($"{serviceUrl}?q={encodedQuery}", this.cancelSource.Token);
                NuGetSearchResult[] results = resultsRoot.data;
                return results;
            }

            return Array.Empty<NuGetSearchResult>();
        }
    }
}
