using DevPrompt.Utility.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Utility
{
    /// <summary>
    /// One shared HttpClient across the process, imported by plugins using Api.IHttpClient
    /// Exported as a singleton from Plugins/ExportProvider.cs since the instance is owned by the App
    /// </summary>
    internal class HttpClientHelper : Api.IHttpClient, IDisposable
    {
        public HttpClient Client { get; }

        public HttpClientHelper()
        {
            this.Client = new HttpClient();
        }

        public void Dispose()
        {
            this.Client.Dispose();
        }

        public async Task<Api.IJsonValue> GetJsonAsync(string uri, CancellationToken cancelToken)
        {
            HttpResponseMessage response = await this.Client.GetAsync(uri, cancelToken);
            response = response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonParser.Parse(json);
        }

        public async Task<dynamic> GetJsonAsDynamicAsync(string uri, CancellationToken cancelToken)
        {
            Api.IHttpClient client = this;
            Api.IJsonValue value = await client.GetJsonAsync(uri, cancelToken);
            return value.Dynamic;
        }

        public async Task<T> GetJsonAsTypeAsync<T>(string uri, CancellationToken cancelToken)
        {
            Api.IHttpClient client = this;
            Api.IJsonValue value = await client.GetJsonAsync(uri, cancelToken);
            return value.Convert<T>();
        }
    }
}
