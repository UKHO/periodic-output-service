using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class AzureStorageConfiguration
    {
        public string ConnectionString { get; set; }

        public string StorageContainerName { get; set; }
    }
}
