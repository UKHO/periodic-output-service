using Azure.Data.Tables;

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
        public static HttpResponseMessage ConfigureFt(string? baseUrl, string? configurationOption)
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

        /// <summary>
        /// This method is use to give time to BuilderService to download the exchangeSet.
        /// </summary>
        public static void WaitForDownloadExchangeSet()
        {
            //The below sleep is to give time to BuilderService to download the exchangeSet.
            Thread.Sleep(210000);
        }

        /// <summary>
        /// This method is used to generate random number
        /// </summary>
        /// <returns></returns>
        public static int RandomNumber()
        {
            Random rnd = new Random();
            return rnd.Next(00000, 99999);
        }

        /// <summary>
        /// This Method is use to delete bessproductversiondetails azure table entries.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <param name="products"></param>
        /// <param name="exchangeSetStandard"></param>
        public static async Task DeleteTableEntries(string? connectionString, string? tableName, List<string>? products, string? exchangeSetStandard)
        {
            TableClient tableClient = new(connectionString, tableName);

            foreach (string product in products!)
            {
                await tableClient.DeleteEntityAsync("BESConfig", exchangeSetStandard + "|" + product);
            }
        }
    }
}
