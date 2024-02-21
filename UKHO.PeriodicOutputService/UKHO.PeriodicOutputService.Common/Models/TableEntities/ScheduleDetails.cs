using Azure;
using Azure.Data.Tables;

namespace UKHO.PeriodicOutputService.Common.Models.TableEntities
{
    public class ScheduleDetails : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime NextScheduleTime { get; set; }
        public bool IsEnabled { get; set; }
        public ETag ETag { get; set; }
    }
}
