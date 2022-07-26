using System.Text;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class EssApiClient : IEssApiClient
    {
        private readonly HttpClient _httpClient;

        public EssApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.MaxResponseContentBufferSize = 2147483647;
            _httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
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
