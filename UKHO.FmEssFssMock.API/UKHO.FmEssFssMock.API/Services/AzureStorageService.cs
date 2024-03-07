using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using UKHO.FmEssFssMock.API.Models.Bess;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;

namespace UKHO.FmEssFssMock.API.Services
{
    public class AzureStorageService
    {
        private readonly BessStorageConfiguration _bessStorageConfiguration;

        public AzureStorageService(IOptions<BessStorageConfiguration> options)
        {
            _bessStorageConfiguration = options.Value;
        }

        public async Task<string> UploadConfigurationToBlob(List<BessConfig> bessConfig)
        {
            string serializeJsonObject = JsonConvert.SerializeObject(bessConfig);
            using var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            await writer.WriteAsync(serializeJsonObject);
            await writer.FlushAsync();
            ms.Position = 0;

            return await UploadStreamToBlob(ms);
        }

        private async Task<string> UploadStreamToBlob(Stream ms)
        {
            var guid = Guid.NewGuid();
            BlobContainerClient blobContainerClient = new(_bessStorageConfiguration.ConnectionString, _bessStorageConfiguration.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);

            BlobClient blobClient = blobContainerClient.GetBlobClient(guid + ".json");
            await blobClient.UploadAsync(ms, new BlobHttpHeaders { ContentType = "application/json" });

            return await blobClient.ExistsAsync() ? guid.ToString() : "";
        }
    }
}
