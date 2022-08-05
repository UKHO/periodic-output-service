namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetHttpResponseMessage
    {
        private static readonly HttpClient httpClient = new();

        public async Task<HttpResponseMessage> GetHttpResponse(string url, string accessToken)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
