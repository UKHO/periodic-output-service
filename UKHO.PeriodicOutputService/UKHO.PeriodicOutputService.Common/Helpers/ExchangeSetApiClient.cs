using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Factories;
using UKHO.PeriodicOutputService.Common.Providers;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class ExchangeSetApiClient : IExchangeSetApiClient
    {
        private readonly IHttpClientFacade _httpClient;

        public ExchangeSetApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(false);
        }

        public async Task<HttpResponseMessage> GetProductIdentifiersDataAsync(string baseUrl, List<string> productIdentifierModel, string accessToken)
        {
            string uri = $"{baseUrl}/productData/productIdentifiers";

            string payloadJson = JsonConvert.SerializeObject(productIdentifierModel);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            })
            {
                if (accessToken != null)
                {
                    httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
