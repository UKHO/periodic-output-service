namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<List<string>> GetJsonStringListFromBlobStorageContainer();
    }
}
