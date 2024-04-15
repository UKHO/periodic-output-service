namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class Extensions
    {
        static readonly HttpClient httpClient = new();

        /// <summary>
        /// This method is used to set the test scenario.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="configurationOption"></param>
        /// <returns></returns>
        public static HttpResponseMessage ConfigureFt(string? baseUrl, string configurationOption)
        {
            string uri = $"{baseUrl}/configurefm/{configurationOption}";

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            return httpClient.Send(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// This method is used to delete the temp folder.
        /// </summary>
        /// <param name="tempFolder"></param>
        public static void DeleteTempDirectory(string? tempFolder)
        {
            string path = Path.GetTempPath() + tempFolder;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
