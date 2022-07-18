namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FssApiClient : IFssApiClient
    {
        private readonly HttpClient _httpClient;

        public FssApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<HttpResponseMessage> GetBatchStatusAsync(string uri, string accessToken)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
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
