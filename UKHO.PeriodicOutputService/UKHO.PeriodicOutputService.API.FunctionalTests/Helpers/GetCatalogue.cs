using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetCatalogue
    {
        public async Task<HttpResponseMessage> GetCatalogueEndpoint(string baseUrl, string accessToken, string subscriptionKey)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));

            string uri = $"{baseUrl}/catalogues/1";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

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
