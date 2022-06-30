using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has actual http calls 
    public class ExchangeSetApiClient : IExchangeSetApiClient
    {
        private readonly HttpClient _httpClient;

        public ExchangeSetApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
