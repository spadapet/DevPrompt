using DevPrompt.Utility.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DevPrompt.Utility.NuGet
{
    internal class NuGetServiceIndex : IDisposable
    {
        public const string NuGetOrg = "https://api.nuget.org/v3/index.json";
        private Dictionary<string, List<string>> serviceToUrls;

        public static async Task<NuGetServiceIndex> Create(HttpClient httpClient, string url = NuGetServiceIndex.NuGetOrg)
        {
            NuGetServiceIndex nuget = new NuGetServiceIndex();
            await nuget.Initialize(httpClient, url);
            return nuget;
        }

        private NuGetServiceIndex()
        {
            this.serviceToUrls = new Dictionary<string, List<string>>();
        }

        private async Task Initialize(HttpClient httpClient, string url)
        {
            string servicesJson = await httpClient.GetStringAsync(url);
            Api.IJsonValue servicesRoot = JsonParser.Parse(servicesJson);

            foreach (Api.IJsonValue service in servicesRoot["resources"].Array)
            {
                string serviceName = service["@type"].String;
                string serviceUrl = service["@id"].String;

                if (!this.serviceToUrls.TryGetValue(serviceName, out List<string> urls))
                {
                    urls = new List<string>();
                    this.serviceToUrls.Add(serviceName, urls);
                }

                urls.Add(serviceUrl);
            }
        }

        public void Dispose()
        {
        }
    }
}
