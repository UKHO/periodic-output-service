using System.Text;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class PksApiClient : IPksApiClient
    {
        private readonly HttpClient httpClient;

        public PksApiClient(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> PostPksDataAsync(string uri, string requestBody, string accessToken, string? correlationId = null)
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequestMessage.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
                httpRequestMessage.AddHeader("X-Correlation-ID", correlationId);
            }

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
