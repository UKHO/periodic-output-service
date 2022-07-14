
namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FleetManagerApiClient : IFleetManagerApiClient
    {
        private readonly HttpClient _httpClient;

        public FleetManagerApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.MaxResponseContentBufferSize = 2147483647;
            _httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> GetJwtAuthUnpToken(HttpMethod method, string baseUrl, string base64Credentials, string subscriptionKey)
        {
            string uri = $"{baseUrl}/auth/unp";

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.CorrelationID.ToString());

            if (!string.IsNullOrEmpty(base64Credentials))
            {
                httpRequestMessage.AddHeader("userPass", base64Credentials);
            }
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                httpRequestMessage.AddHeader("Ocp-Apim-Subscription-Key", subscriptionKey);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> GetCatalogue(HttpMethod method, string baseUrl, string accessToken, string subscriptionKey)
        {
            string uri = $"{baseUrl}/catalogues/1";

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.CorrelationID.ToString());

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.AddHeader("token", accessToken);
            }
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                httpRequestMessage.AddHeader("Ocp-Apim-Subscription-Key", subscriptionKey);
            }
            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
