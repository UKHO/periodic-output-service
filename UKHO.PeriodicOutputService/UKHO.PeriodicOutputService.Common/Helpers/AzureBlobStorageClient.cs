using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureBlobStorageClient : IAzureBlobStorageClient
    {
        private readonly BessStorageConfiguration bessStorageConfiguration;

        public AzureBlobStorageClient(IOptions<BessStorageConfiguration> bessStorageConfiguration)
        {
            this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
        }

        public Dictionary<string, string> GetConfigsInContainer()
        {
            Dictionary<string, string> configs = new();

            BlobContainerClient blobContainerClient = GetBlobContainerClient();

            foreach (BlobItem blobItem in blobContainerClient.GetBlobs())
            {
                if (blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    BlobClient blobClient = GetBlobClient(blobContainerClient, blobItem.Name);
                    configs.Add(blobItem.Name, DownloadBlobContent(blobClient));
                }
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
    }
}
