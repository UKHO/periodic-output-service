namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetBatchStatus
    {
        static HttpClient httpClient = new HttpClient();

        public async Task<HttpResponseMessage> GetBatchStatusAsync(string baseURL, string batchStatusURI, string accessToken)
        {
            var responseUri = new UriBuilder(batchStatusURI);

            var batchId = responseUri.Uri.Segments.FirstOrDefault(d => Guid.TryParse(d.Replace("/", ""), out var _));

            string uri = $"{baseURL}/batch/{batchId}status";

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
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
