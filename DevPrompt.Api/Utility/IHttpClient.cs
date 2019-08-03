using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevPrompt.Api
{
    /// <summary>
    /// Shares a single HttpClient in the process
    /// </summary>
    public interface IHttpClient
    {
        HttpClient Client { get; }

        Task<dynamic> GetJsonAsDynamicAsync(string uri, CancellationToken cancelToken);
        Task<T> GetJsonAsTypeAsync<T>(string uri, CancellationToken cancelToken);
    }
}
