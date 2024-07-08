namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<Dictionary<string, string>> GetConfigsInContainerAsync();
    }
}
