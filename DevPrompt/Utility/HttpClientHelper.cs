using Efficient.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Utility
{
    internal class HttpClientHelper : IDisposable
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
            return JsonValue.StringToValue(json);
        }

        public async Task<T> GetJsonAsTypeAsync<T>(string uri, CancellationToken cancelToken)
        {
            string json = await this.GetStringAsync(uri, cancelToken);
            return JsonValue.StringToObject<T>(json);
        }

        private async Task<string> GetStringAsync(string uri, CancellationToken cancelToken)
        {
            HttpResponseMessage response = await this.Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            response = response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
