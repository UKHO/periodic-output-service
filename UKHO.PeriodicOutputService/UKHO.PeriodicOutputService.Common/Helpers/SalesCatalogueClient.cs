using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private readonly HttpClient httpClient;

        public SalesCatalogueClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string requestBody, string authToken, string uri)
        {
            HttpContent content = null;

            if (requestBody != null)
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            using var httpRequestMessage = new HttpRequestMessage(method, uri)
            { Content = content };

            httpRequestMessage.Headers.Add("X-Correlation-ID", CommonHelper.CorrelationID.ToString());

            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
