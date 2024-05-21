using System.Text;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Models.Ess;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class EssApiClient : IEssApiClient
    {
        private readonly HttpClient _httpClient;
        private const string ESSCLIENT = "EssClient";

        public EssApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(ESSCLIENT);
            _httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> PostProductIdentifiersDataAsync(string uri, List<string> productIdentifierModel, string accessToken, string? correlationId = null)
        {
            string payloadJson = JsonConvert.SerializeObject(productIdentifierModel);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            })
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                    httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.GetCorrelationId(correlationId));
                }
                return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        public async Task<HttpResponseMessage> GetProductDataSinceDateTime(string uri, string sinceDateTime, string accessToken, string? correlationId = null)
        {
            string payloadJson = JsonConvert.SerializeObject(sinceDateTime);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            })
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                    httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.GetCorrelationId(correlationId));
                }

                return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        public async Task<HttpResponseMessage> GetProductDataProductVersion(string uri, List<ProductVersion> productVersions, string accessToken, string? correlationId = null)
        {
            string payloadJson = JsonConvert.SerializeObject(productVersions);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            })
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                    httpRequestMessage.AddHeader("X-Correlation-ID", CommonHelper.GetCorrelationId(correlationId));
                }

                return await _httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
