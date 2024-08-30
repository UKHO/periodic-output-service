using System.Diagnostics.CodeAnalysis;
using Azure;
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

        public async Task<Dictionary<string, string>> GetConfigsInContainerAsync()
        {
            Dictionary<string, string> configs = new();

            try
            {
                BlobContainerClient blobContainerClient = await GetBlobContainerClientAsync();

                await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        BlobClient blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
                        configs.Add(blobItem.Name, await DownloadBlobContentAsync(blobClient));
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

        private async Task<BlobContainerClient> GetBlobContainerClientAsync()
        {
            BlobContainerClient blobContainerClient = new(bessStorageConfiguration.ConnectionString, bessStorageConfiguration.ContainerName);

            try
            {
                Response<BlobContainerInfo>? containerCreated = await blobContainerClient.CreateIfNotExistsAsync();

                if (containerCreated != null && containerCreated.HasValue)
                {
                    logger.LogInformation(EventIds.BlobContainerCreated.ToEventId(),
                        $"Created blob container {bessStorageConfiguration.ContainerName}");
                    this.logger.LogInformation(EventIds.BlobContainerCreated.ToEventId(),
                        "Blob created:{blob} and _X-Correlation-ID:{CorrelationId}",
                        bessStorageConfiguration.ContainerName, CommonHelper.CorrelationID);
                }
            }
            catch (Exception e)
            {
                this.logger.LogError("Error occurred retrieving or creating blob with connection string {connectionString} and name {containersName}", bessStorageConfiguration.ConnectionString, bessStorageConfiguration.ContainerName);
                this.logger.LogError("Stack trace {message}, {inner}", e.Message, e.InnerException);
            }

            return blobContainerClient;
        }

        private static BlobClient GetBlobClient(BlobContainerClient blobContainerClient, string blobName)
        {
            return blobContainerClient.GetBlobClient(blobName);
        }

        private static async Task<string> DownloadBlobContentAsync(BlobClient blobClient)
        {
            BlobDownloadInfo response = await blobClient.DownloadAsync();

            using var reader = new StreamReader(response.Content);
            return await reader.ReadToEndAsync();
        }

        public async Task<Dictionary<string, string>> DeleteConfigsInContainer()
        {
            Dictionary<string, string> configs = new();

            try
            {
                BlobContainerClient blobContainerClient = await GetBlobContainerClientAsync();

                await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        BlobClient blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
                        await blobClient.DeleteIfExistsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.ConfigDeleteException.ToEventId(), "Exception occurred while deleting configs from azure storage | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.ConfigDeleteException.ToEventId());
            }
            return configs;
        }
    }
}
