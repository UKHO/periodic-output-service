using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Fulfilment.Models.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AzureStorageConfiguration : IAzureStorageConfiguration
    {
        public string StorageAccountName { get; set; } = "Default Key";
        public string StorageAccountKey { get; set; } = "Default Key";
        public string QueueName { get; set; } = "Default Key";
    }
}
