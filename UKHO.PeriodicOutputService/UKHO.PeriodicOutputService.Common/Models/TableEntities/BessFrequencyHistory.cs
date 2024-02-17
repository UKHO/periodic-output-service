using Azure;
using Azure.Data.Tables;

namespace UKHO.PeriodicOutputService.Common.Models.TableEntities
{
    public class BessFrequencyHistory : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;
        public string Name { get; set; }
        public string Frequency { get; set; }
        public ETag ETag { get; set; }
    }
}
