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
        private readonly BESSStorageConfiguration bessStorageConfiguration;

        public AzureBlobStorageClient(IOptions<BESSStorageConfiguration> bessStorageConfiguration)
        {
            this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
        }

        public async Task<List<string>> GetJsonStringListFromBlobStorageContainer()
        {
            List<string> jsonStringList = new();
            string azureStorageConnectionString = bessStorageConfiguration.ConnectionString;
            string containerName = bessStorageConfiguration.StorageContainerName;

            BlobServiceClient blobServiceClient = new(azureStorageConnectionString);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            bool isExist = await containerClient.ExistsAsync();

            if (!isExist)
            {
                throw new Exception("Container does not exists");
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
