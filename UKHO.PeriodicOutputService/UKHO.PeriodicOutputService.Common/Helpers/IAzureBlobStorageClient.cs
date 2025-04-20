using Azure.Storage.Blobs;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<Dictionary<string, string>> GetConfigsInContainerAsync();

        Task<BlobClient> GetBlobClientAsync(string containerName, string blobName);

        Task<BlobClient> GetBlobClientByUriAsync(string uri);

        Task<string> DownloadBlobContentAsync(BlobClient blobClient);

        Task DeleteBlobContentAsync(BlobClient blobClient);
    }
}
