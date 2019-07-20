using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Utility
{
    /// <summary>
    /// One shared HttpClient across the process, imported by plugins using Api.IHttpClient
    /// </summary>
    internal class HttpClientWrapper : Api.IHttpClient, IDisposable
    {
        public HttpClient Client { get; }

        public HttpClientWrapper()
        {
            this.Client = new HttpClient();
        }


        public void Dispose()
        {
            this.Client.Dispose();
        }

        public Task<Api.IJsonValue> GetJsonAsync(string uri, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
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
