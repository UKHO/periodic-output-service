using UKHO.PeriodicOutputService.Common.Factories;
using UKHO.PeriodicOutputService.Common.Providers;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FleetManagerClient : IFleetManagerClient
    {
        private readonly IHttpClientFacade _httpClient;

        public FleetManagerClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(true);
        }

        public async Task<HttpResponseMessage> GetJwtAuthUnpToken(HttpMethod method, string baseUrl, string base64Credentials, string subscriptionKey)
        {
            string uri = $"{baseUrl}/auth/unp";

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            if (!string.IsNullOrEmpty(base64Credentials))
            {
                httpRequestMessage.Headers.Add("userPass", base64Credentials);
            }
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            }

            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> GetCatalogue(HttpMethod method, string baseUrl, string accessToken, string subscriptionKey)
        {
            string uri = $"{baseUrl}/catalogues/1";

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.Add("token", accessToken);
            }
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            }
            return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
