namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Dictionary<string, string> GetConfigsInContainer();
    }
}
