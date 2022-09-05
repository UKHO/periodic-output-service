using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Data.Tables;

namespace UKHO.PeriodicOutputService.Common.Models.TableEntities
{
    [ExcludeFromCodeCoverage]
    public class WebJobHistory : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime SinceDateTime { get; set; }
        public bool IsJobSuccess { get; set; }
        public ETag ETag { get; set; }
    }
}
