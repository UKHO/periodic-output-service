using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Storage;
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

        // Constructor added to use in FT.
        public AzureBlobStorageClient(IOptions<BessStorageConfiguration> bessStorageConfiguration)
        {
            this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
        }

        public async Task<Dictionary<string, string>> GetConfigsInContainerAsync()
        {
            Dictionary<string, string> configs = new();

            try
            {
                var blobContainerClient = await GetBlobContainerClientAsync(bessStorageConfiguration.ContainerName);

                await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
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

        private async Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName)
        {
            BlobContainerClient blobContainerClient = new(bessStorageConfiguration.ConnectionString, containerName);

            try
            {
                var containerCreated = await blobContainerClient.CreateIfNotExistsAsync();

                if (containerCreated is { HasValue: true })
                {
                    logger.LogInformation(EventIds.BlobContainerCreated.ToEventId(),
                        $"Created blob container {containerName}");
                    logger.LogInformation(EventIds.BlobContainerCreated.ToEventId(),
                        "Blob created:{blob} and _X-Correlation-ID:{CorrelationId}",
                        containerName, CommonHelper.CorrelationID);
                }
            }
            catch (Exception e)
            {
                logger.LogError(EventIds.ContainerCreationFailure.ToEventId(), "Error occurred retrieving or creating blob with connection string {connectionString} and name {containersName} and _X-Correlation-ID:{correlationId}", bessStorageConfiguration.ConnectionString, bessStorageConfiguration.ContainerName, CommonHelper.CorrelationID);
                logger.LogError(EventIds.ContainerCreationFailure.ToEventId(), "Stack trace {message}, {inner} and _X-Correlation-ID:{correlationId}", e.Message, e.InnerException, CommonHelper.CorrelationID);
            }

            return blobContainerClient;
        }

        private static BlobClient GetBlobClient(BlobContainerClient blobContainerClient, string blobName)
        {
            return blobContainerClient.GetBlobClient(blobName);
        }

        public async Task<BlobClient> GetBlobClientAsync(string containerName, string blobName)
        {
            return (await GetBlobContainerClientAsync(containerName)).GetBlobClient(blobName);
        }

        public BlobClient GetBlobClientByUriAsync(string uri)
        {
            var blobServiceClient = new BlobServiceClient(bessStorageConfiguration.ConnectionString);

            var blobUriBuilder = new BlobUriBuilder(new Uri(uri));

            var containerClient = blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);

            return containerClient.GetBlobClient(blobUriBuilder.BlobName);
        }

        public async Task<string> DownloadBlobContentAsync(BlobClient blobClient)
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
                var blobContainerClient = await GetBlobContainerClientAsync(bessStorageConfiguration.ContainerName);

                await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
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

        public async Task DeleteBlobContentAsync(BlobClient blobClient)
        {
            await blobClient.DeleteIfExistsAsync();
        }

    }
}
