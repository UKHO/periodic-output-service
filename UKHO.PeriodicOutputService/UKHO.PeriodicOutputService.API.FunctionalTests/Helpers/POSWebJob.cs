
namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class POSWebJob
    {
        static readonly HttpClient httpClient = new HttpClient();

        public async Task<HttpResponseMessage> POSWebJobEndPoint(string baseUrl, string base64Credentials)
        {
            string uri = $"{baseUrl}/run";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            if (!string.IsNullOrEmpty(base64Credentials)) { httpRequestMessage.Headers.Add("Authorization", "Basic " + base64Credentials); }
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
