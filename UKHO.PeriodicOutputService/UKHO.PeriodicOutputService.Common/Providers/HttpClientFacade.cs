using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace UKHO.PeriodicOutputService.Common.Providers
{
    public interface IHttpClientFacade : IDisposable
    {
        Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> GetAsync(Uri uri, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken = default);
        HttpRequestHeaders DefaultRequestHeaders { get; }
    }

    [ExcludeFromCodeCoverage]
    public class HttpClientFacade : IHttpClientFacade
    {
        private readonly HttpClient _client;

        public HttpClientFacade(HttpClient client, bool enableTimeOut)
        {
            _client = client;

            if (enableTimeOut)
            {
                _client.MaxResponseContentBufferSize = 2147483647;
                _client.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content, CancellationToken cancellationToken = default)
        {
            return _client.PostAsync(uri, content, cancellationToken);
        }

        public Task<HttpResponseMessage> GetAsync(Uri uri, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
        {
            return _client.GetAsync(uri, completionOption, cancellationToken);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken = default)
        {
            return _client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }

        public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

    }
}
