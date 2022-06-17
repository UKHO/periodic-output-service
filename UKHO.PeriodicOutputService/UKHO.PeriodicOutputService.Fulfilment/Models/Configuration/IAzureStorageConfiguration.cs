namespace UKHO.PeriodicOutputService.Fulfilment.Models.Configuration
{
    public interface IAzureStorageConfiguration
    {
        string StorageAccountName { get; set; }
        string StorageAccountKey { get; set; }
        string QueueName { get; set; }
    }
}
