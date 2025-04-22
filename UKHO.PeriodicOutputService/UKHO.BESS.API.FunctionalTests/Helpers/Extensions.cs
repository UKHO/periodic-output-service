using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class Extensions
    {
        static readonly HttpClient httpClient = new();
        static readonly TestConfiguration testConfiguration = new();
        static readonly string[] exchangeSetStandards = { testConfiguration.bessConfig.s57ExchangeSetStandard!, testConfiguration.bessConfig.s63ExchangeSetStandard! };

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
        public static async Task DeleteTableEntries(string? connectionString, string? tableName, List<string>? products)
        {
            TableClient tableClient = new(connectionString, tableName);

            foreach (var exchangeSetStandard in exchangeSetStandards)
            {
                foreach (var product in products!)
                {
                    await tableClient.DeleteEntityAsync("PortOfIndia", exchangeSetStandard + "|" + product);
                }
            }
        }

        /// <summary>
        /// This method is use to add the queue message.
        /// </summary>
        /// <param name="type">Sets the type in the queueMessage as per the config</param>
        /// <param name="exchangeSetStandard">Sets the value as s63 or s57 of the exchangeSetStandard as per config</param>
        /// <param name="webjobConnectionString">Sets the connectionString of the webJob</param>
        /// <param name="queueName">Sets the name of the table for queue</param>
        /// <param name="messageDetailUri">Sets the uri of message detail blob storage</param>
        /// <param name="keyFileType">Sets the Permit file type for the queue. Default is set to NONE</param>
        public static void AddQueueMessage(string type, string? exchangeSetStandard, string? webjobConnectionString, string? queueName, string? keyFileType = "NONE")
        {
            var messageDetailUri = StoreMessageDetail(webjobConnectionString);
            var queueMessage = JsonConvert.DeserializeObject<ConfigQueueMessage>(File.ReadAllText("./TestData/BSQueueMessage.txt"));
            queueMessage!.Type = type;
            queueMessage.ExchangeSetStandard = exchangeSetStandard!;
            queueMessage.KeyFileType = keyFileType!;
            queueMessage.MessageDetailUri = messageDetailUri!;
            var jsonString = JsonConvert.SerializeObject(queueMessage);

            QueueClientOptions queueOptions = new() { MessageEncoding = QueueMessageEncoding.Base64 };
            QueueClient queue = new(webjobConnectionString, queueName, queueOptions);
            queue.SendMessage(jsonString);
        }

        private static string StoreMessageDetail(string? webJobConnectionString)
        {
            var fileName = "BSQueueMessageDetail";
            var blobName = $"{fileName}{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var messageDetail = JsonConvert.DeserializeObject<MessageDetail>(File.ReadAllText($"./TestData/{fileName}.txt"));
            var jsonString = JsonConvert.SerializeObject(messageDetail);
            using var ms = new MemoryStream();

            LoadStreamWithJson(ms, jsonString);
            BlobClient blobClient = new(webJobConnectionString, testConfiguration.bessStorageConfig.MessageContainerName, blobName);
            blobClient.Upload(ms);

            return blobClient.Uri.AbsoluteUri;
        }

        private static void LoadStreamWithJson(Stream ms, object obj)
        {
            var writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
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

        /// <summary>
        /// This method reads response body json as given type
        /// </summary>
        /// <typeparam name="T">Sets the type</typeparam>
        /// <param name="httpResponseMessage">Sets the response message</param>
        /// <returns></returns>
        public static async Task<T> ReadAsTypeAsync<T>(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(bodyJson)!;
        }
    }
}
