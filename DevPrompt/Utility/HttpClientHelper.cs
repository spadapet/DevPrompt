using Efficient.Json;
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

        public async Task<dynamic> GetJsonAsDynamicAsync(string uri, CancellationToken cancelToken)
        {
            string json = await this.GetStringAsync(uri, cancelToken);
            return JsonValue.Parse(json);
        }

        public async Task<T> GetJsonAsTypeAsync<T>(string uri, CancellationToken cancelToken)
        {
            string json = await this.GetStringAsync(uri, cancelToken);
            return JsonValue.Deserialize<T>(json);
        }

        private async Task<string> GetStringAsync(string uri, CancellationToken cancelToken)
        {
            HttpResponseMessage response = await this.Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            response = response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
