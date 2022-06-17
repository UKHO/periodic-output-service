using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Fulfilment.Models.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AzureStorageConfiguration : IAzureStorageConfiguration
    {
        public string StorageAccountName { get; set; } = string.Empty;
        public string StorageAccountKey { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
    }
}
