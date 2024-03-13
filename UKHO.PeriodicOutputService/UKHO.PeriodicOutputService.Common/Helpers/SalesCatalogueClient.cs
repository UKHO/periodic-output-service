using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private HttpClient httpClient;

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient();
            httpClient.MaxResponseContentBufferSize = 2147483647;
            httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string requestBody, string authToken, string uri)
        {
            HttpContent content = null;

            if (requestBody != null)
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = content };

            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());
            httpRequestMessage.SetBearerToken(authToken);

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
