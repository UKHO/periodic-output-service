using System.Text;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class ExchangeSetApiClient : IExchangeSetApiClient
    {
        private readonly HttpClient _httpClient;

        public ExchangeSetApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<HttpResponseMessage> PostProductIdentifiersDataAsync(string baseUrl, List<string> productIdentifierModel, string accessToken)
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
                    httpRequestMessage.SetBearerToken(accessToken);
                    httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.CorrelationID.ToString());
                }

                return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
