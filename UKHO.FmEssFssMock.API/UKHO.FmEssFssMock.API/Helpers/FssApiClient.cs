using System.Text;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;

namespace UKHO.FmEssFssMock.API.Helpers
{
    public class FssApiClient
    {
        private readonly IOptions<FileShareServiceConfiguration> _fssConfiguration;
        private readonly HttpClient _httpClient;

        public FssApiClient(IOptions<FileShareServiceConfiguration> fssConfiguration,
                            IHttpClientFactory httpClientFactory)
        {
            _fssConfiguration = fssConfiguration;

            _httpClient = httpClientFactory.CreateClient();
        }

        public HttpResponseMessage CreateBatch(string requestBody)
        {
            string uri = $"{_fssConfiguration.Value.FssStubBaseUrl}/batch";
            HttpContent? content = null;

            if (requestBody != null)
            {
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = content };

            HttpResponseMessage? response = _httpClient.Send(httpRequestMessage, CancellationToken.None);

            return response;
        }

        public HttpResponseMessage AddFiles(string requestBody, string batchId, string fileName, long? fileContentSizeHeader, string mimeTypeHeader = "application/octet-stream")
        {
            string uri = $"{_fssConfiguration.Value.FssStubBaseUrl}/batch/{batchId}/files/{fileName}";

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(requestBody, Encoding.UTF8, "application/json") };

            httpRequestMessage.Headers.Add("X-MIME-Type", mimeTypeHeader);

            if (fileContentSizeHeader.HasValue)
            {
                httpRequestMessage.Headers.Add("X-Content-Size", fileContentSizeHeader.Value.ToString());
            }

            return _httpClient.Send(httpRequestMessage, CancellationToken.None);
        }
    }
}
