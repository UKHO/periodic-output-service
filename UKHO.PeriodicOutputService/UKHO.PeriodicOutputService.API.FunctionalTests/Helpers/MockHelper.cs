namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class MockHelper
    {
        private static readonly HttpClient httpClient = new();
        public static HttpResponseMessage Cleanup(string baseUrl)
        {
            string uri = $"{baseUrl}/cleanUp";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            return httpClient.Send(httpRequestMessage, CancellationToken.None);
        }


        public static HttpResponseMessage ConfigureFM(string baseUrl, string configurationOption)
        {
            string uri = $"{baseUrl}/mock/configurefm/{configurationOption}";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            return httpClient.Send(httpRequestMessage, CancellationToken.None);
        }
    }
}
