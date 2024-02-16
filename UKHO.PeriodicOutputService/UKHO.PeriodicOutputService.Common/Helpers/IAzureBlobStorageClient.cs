namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        List<string> GetConfigsInContainer();
    }
}
