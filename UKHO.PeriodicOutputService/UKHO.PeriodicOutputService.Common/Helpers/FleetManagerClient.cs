using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has actual http calls 
    public class FleetManagerClient : IFleetManagerClient
    {
        private readonly HttpClient httpClient;

        public FleetManagerClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
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
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public async Task<HttpResponseMessage> GetJwtAuthJwtToken(HttpMethod method, string baseUrl, string accessToken, string subscriptionKey)
        {
            string uri = $"{baseUrl}/auth/jwt";

            var httpRequestMessage = new HttpRequestMessage(method, uri);

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.Add("token", accessToken);
            }
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            }
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
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
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
