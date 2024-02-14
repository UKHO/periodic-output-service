using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureBlobStorageClient : IAzureBlobStorageClient
    {
        private readonly AzureStorageConfiguration azureStorageConfiguration;
        private readonly ILogger<AzureBlobStorageClient> logger;
        public AzureBlobStorageClient(IOptions<AzureStorageConfiguration> azureStorageConfiguration, ILogger<AzureBlobStorageClient> logger)
        {
            this.azureStorageConfiguration = azureStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(azureStorageConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<string>> GetJsonStringListFromBlobStorageContainer()
        {
            List<string> jsonStringList = new();
            string azureStorageConnectionString = azureStorageConfiguration.ConnectionString;
            string containerName = azureStorageConfiguration.StorageContainerName;

            BlobServiceClient blobServiceClient = new(azureStorageConnectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            bool isExist = await containerClient.ExistsAsync();

            if (!isExist)
            {
                logger.LogError(EventIds.BESSContainerNotExist.ToEventId(), "Storage container is not exists | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                throw new Exception();
            }
            else
            {
                foreach (BlobItem blobItem in containerClient.GetBlobs())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                        string jsonString = GetJsonFileContent(blobClient);
                        jsonStringList.Add(jsonString);
                    }
                }
            }

            return jsonStringList;
        }

        private static string GetJsonFileContent(BlobClient blobClient)
        {
            BlobDownloadInfo response = blobClient.DownloadAsync().Result;

            using var reader = new StreamReader(response.Content);
            string jsonContent = reader.ReadToEnd();

            return jsonContent;
        }
    }
}
