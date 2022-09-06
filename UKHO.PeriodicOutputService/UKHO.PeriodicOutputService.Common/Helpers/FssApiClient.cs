using System.Net.Http.Headers;
using System.Text;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FssApiClient : IFssApiClient
    {
        private readonly HttpClient _httpClient;

        public FssApiClient(IHttpClientFactory httpClientFactory) => _httpClient = httpClientFactory.CreateClient();

        public async Task<HttpResponseMessage> CreateBatchAsync(string uri, string requestBody, string authToken)
        {
            HttpContent? content = null;

            if (requestBody != null)
            {
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = content };

            httpRequestMessage.SetBearerToken(authToken);
            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());

            HttpResponseMessage? response = await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

            return response;
        }

        public async Task<HttpResponseMessage> AddFileToBatchAsync(string uri, string requestBody, string authToken, long? fileContentSizeHeader, string mimeTypeHeader = "application/octet-stream")
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(requestBody, Encoding.UTF8, "application/json") };

            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());
            httpRequestMessage.AddHeader("X-MIME-Type", mimeTypeHeader);
            httpRequestMessage.SetBearerToken(authToken);

            if (fileContentSizeHeader.HasValue)
            {
                httpRequestMessage.AddHeader("X-Content-Size", fileContentSizeHeader.Value.ToString());
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> UploadFileBlockAsync(string uri, byte[] blockBytes, byte[] md5Hash, string accessToken, string mimeTypeHeader = "application/octet-stream")
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
            { Content = (blockBytes == null) ? null : new ByteArrayContent(blockBytes) };

            if (httpRequestMessage.Content != null) httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());
            httpRequestMessage.SetBearerToken(accessToken);

            if (md5Hash != null && httpRequestMessage.Content != null)
            {
                httpRequestMessage.Content.Headers.ContentMD5 = md5Hash;
            }
            if (mimeTypeHeader != null)
            {
                httpRequestMessage.AddHeader("X-MIME-Type", mimeTypeHeader);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> WriteBlockInFileAsync(string uri, string requestBody, string accessToken, string mimeTypeHeader = "application/octet-stream")
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
            { Content = new StringContent(requestBody, Encoding.UTF8, "application/json") };

            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());
            httpRequestMessage.AddHeader("X-MIME-Type", mimeTypeHeader);
            httpRequestMessage.SetBearerToken(accessToken);

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> CommitBatchAsync(string uri, string requestBody, string accessToken)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
            { Content = new StringContent(requestBody, Encoding.UTF8, "application/json") };

            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());
            httpRequestMessage.SetBearerToken(accessToken);

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> GetBatchDetailsAsync(string uri, string accessToken)
        {
            return await CallFSSApi(uri, accessToken);
        }

        public async Task<HttpResponseMessage> GetBatchStatusAsync(string uri, string accessToken)
        {
            return await CallFSSApi(uri, accessToken);
        }

        public async Task<HttpResponseMessage> DownloadFile(string uri, string accessToken, string rangeHeader)
        {
            return await CallFSSApi(uri, accessToken, rangeHeader);
        }

        private async Task<HttpResponseMessage> CallFSSApi(string uri, string accessToken, string? rangeHeader = null)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            httpRequestMessage.SetBearerToken(accessToken);
            httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.CorrelationID.ToString());
            if (!string.IsNullOrEmpty(rangeHeader))
                httpRequestMessage.Headers.Add("Range", rangeHeader);
            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
