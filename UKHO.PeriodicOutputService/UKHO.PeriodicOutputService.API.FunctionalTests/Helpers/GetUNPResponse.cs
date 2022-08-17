
namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetUNPResponse { 
        static HttpClient httpClient = new HttpClient();

        public async Task<HttpResponseMessage> GetJwtAuthUnpToken(string baseUrl, string base64Credentials, string subscriptionKey)
        {
            string uri = $"{baseUrl}/ft/auth/unp";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

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
    }
}
