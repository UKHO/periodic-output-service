namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class Extensions
    {
        static readonly HttpClient httpClient = new();

        public static HttpResponseMessage ConfigureFM(string? baseUrl, string configurationOption)
        {
            string uri = $"{baseUrl}/configurefm/{configurationOption}";

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            return httpClient.Send(httpRequestMessage, CancellationToken.None);
        }
    }
}
