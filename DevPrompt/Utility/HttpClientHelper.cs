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
            return await this.GetJsonAsync(uri, cancelToken);
        }

        public async Task<T> GetJsonAsTypeAsync<T>(string uri, CancellationToken cancelToken)
        {
            JsonValue value = await this.GetJsonAsync(uri, cancelToken);
            return value.Deserialize<T>();
        }

        private async Task<JsonValue> GetJsonAsync(string uri, CancellationToken cancelToken)
        {
            HttpResponseMessage response = await this.Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            response = response.EnsureSuccessStatusCode();

            // TODO: Detect stream encoding, using streaming parser, and deal with cancellation
            string json = await response.Content.ReadAsStringAsync();
            return JsonValue.Parse(json);
        }
    }
}
