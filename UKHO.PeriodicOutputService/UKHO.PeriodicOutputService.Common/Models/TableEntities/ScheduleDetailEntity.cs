using Azure;
using Azure.Data.Tables;

namespace UKHO.PeriodicOutputService.Common.Models.TableEntities
{
    public class ScheduleDetailEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime NextScheduleTime { get; set; }
        public string IsEnabled { get; set; }
        public bool IsExecuted { get; set; }
        public ETag ETag { get; set; }
    }
}
