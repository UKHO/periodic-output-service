using Azure.Data.Tables;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class Extensions
    {
        private static readonly HttpClient httpClient = new();

        /// <summary>
        /// This method is used to set the test scenario.
        /// </summary>
        /// <param name="baseUrl">Sets the ConfigureFm baseUrl</param>
        /// <param name="configurationOption">Sets the value for configuring FT</param>
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
        /// <param name="tempFolder">Sets the temp folder path to download and check the Exchange Sets and contents</param>
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
        /// This Method is use to delete bessproductversiondetails azure table entries.
        /// </summary>
        /// <param name="connectionString">Sets the connectionString of the storage account</param>
        /// <param name="tableName">Sets the name of the table from which entries is to be removed</param>
        /// <param name="products">Sets the products for which entry is to be deleted</param>
        /// <param name="exchangeSetStandard">Sets the exchangeSetStandard entries of whose is to be deleted</param>
        public static async Task DeleteTableEntries(string? connectionString, string? tableName, List<string>? products, string? exchangeSetStandard)
        {
            TableClient tableClient = new(connectionString, tableName);

            foreach (string product in products!)
            {
                await tableClient.DeleteEntityAsync("BESConfig", exchangeSetStandard + "|" + product);
            }
        }

        /// <summary>
        /// This method is use to add the queue message.
        /// </summary>
        /// <param name="type">Sets the type in the queueMessage as per the config</param>
        /// <param name="exchangeSetStandard">Sets the value as s63 or s57 of the exchangeSetStandard as per config</param>
        /// <param name="webjobConnectionString">Sets the connectionString of the webJob</param>
        /// <param name="queueName">Sets the name of the table for queue</param>
        public static void AddQueueMessage(string type, string? exchangeSetStandard, string? webjobConnectionString, string? queueName)
        {
            var queueMessage = JsonConvert.DeserializeObject<ConfigQueueMessage>(File.ReadAllText("./TestData/BSQueueMessage.txt"));
            queueMessage!.Type = type;
            queueMessage.ExchangeSetStandard = exchangeSetStandard!;
            string jsonString = JsonConvert.SerializeObject(queueMessage);

            QueueClientOptions queueOptions = new() { MessageEncoding = QueueMessageEncoding.Base64 };
            QueueClient queue = new(webjobConnectionString, queueName, queueOptions);
            queue.SendMessage(jsonString);
        }

        /// <summary>
        /// This method is use to clean the POS folder.
        /// </summary>
        /// <param name="baseUrl">Sets the baseUrl for cleanUp</param>
        /// <returns></returns>
        public static HttpResponseMessage Cleanup(string? baseUrl)
        {
            string uri = $"{baseUrl}/cleanUp";
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            return httpClient.Send(httpRequestMessage, CancellationToken.None);
        }
    }
}
