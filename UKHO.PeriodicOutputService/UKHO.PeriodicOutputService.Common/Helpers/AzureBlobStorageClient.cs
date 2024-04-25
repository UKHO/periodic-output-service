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
        private readonly BessStorageConfiguration bessStorageConfiguration;
        private readonly ILogger<AzureBlobStorageClient> logger;

        public AzureBlobStorageClient(IOptions<BessStorageConfiguration> bessStorageConfiguration, ILogger<AzureBlobStorageClient> logger)
        {
            this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Constructer added to use in FT.
        public AzureBlobStorageClient(IOptions<BessStorageConfiguration> bessStorageConfiguration)
        {
            this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
        }

        public Dictionary<string, string> GetConfigsInContainer()
        {
            Dictionary<string, string> configs = new();

            try
            {
                BlobContainerClient blobContainerClient = GetBlobContainerClient();

                foreach (BlobItem blobItem in blobContainerClient.GetBlobs())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        BlobClient blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
                        configs.Add(blobItem.Name, DownloadBlobContent(blobClient));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessErrorOccurredWhileDownloadingConfigFromAzureStorage.ToEventId(), "Exception occurred while downloading configs from azure storage with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
            }
            return configs;
        }

        //Private Methods

        private BlobContainerClient GetBlobContainerClient()
        {
            BlobContainerClient blobContainerClient = new(bessStorageConfiguration.ConnectionString, bessStorageConfiguration.ContainerName);

            bool isExist = blobContainerClient.Exists();

            if (!isExist)
            {
                throw new Exception("Container does not exists");
            }
            return blobContainerClient;
        }

        private static BlobClient GetBlobClient(BlobContainerClient blobContainerClient, string blobName)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
            return blobClient;
        }

        private static string DownloadBlobContent(BlobClient blobClient)
        {
            BlobDownloadInfo response = blobClient.DownloadAsync().Result;

            using var reader = new StreamReader(response.Content);
            return reader.ReadToEnd();
        }

        public Dictionary<string, string> DeleteConfigsInContainer()
        {
            Dictionary<string, string> configs = new();

            try
            {
                BlobContainerClient blobContainerClient = GetBlobContainerClient();

                foreach (BlobItem blobItem in blobContainerClient.GetBlobs())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        BlobClient blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
                        blobClient.DeleteIfExistsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Exception occurred while deleting configs from azure storage");
            }
            return configs;
        }
    }
}
