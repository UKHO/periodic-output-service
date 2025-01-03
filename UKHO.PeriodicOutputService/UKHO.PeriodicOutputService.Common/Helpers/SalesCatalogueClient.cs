﻿using System.Text;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private HttpClient httpClient;
        private const string SCSCLIENT = "ScsClient";

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient(SCSCLIENT);
            httpClient.MaxResponseContentBufferSize = 2147483647;
            httpClient.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
        }

        public async Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string requestBody, string authToken, string uri)
        {
            HttpContent content = null;

            if (requestBody != null)
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            using var httpRequestMessage = new HttpRequestMessage(method, uri);
            httpRequestMessage.Content = content;

            httpRequestMessage.AddCorrelationId(CommonHelper.CorrelationID.ToString());
            httpRequestMessage.SetBearerToken(authToken);

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
