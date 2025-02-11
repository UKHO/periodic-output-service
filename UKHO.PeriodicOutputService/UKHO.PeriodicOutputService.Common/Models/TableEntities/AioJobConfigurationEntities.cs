using Azure;
using Azure.Data.Tables;

namespace UKHO.PeriodicOutputService.Common.Models.TableEntities
{
    public class AioJobConfigurationEntities : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string BusinessUnit { get; set; }
        public string ReadUsers { get; set; }
        public string ReadGroups { get; set; }
        public bool IsEnabled { get; set; }
        public int? WeeksToIncrement { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;
        public ETag ETag { get; set; }
    }
}
