using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Data.Tables;

namespace UKHO.PeriodicOutputService.Common.Models.TableEntities
{
    [ExcludeFromCodeCoverage]
    public class BessProductVersionEntities : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; } = DateTime.UtcNow;
        public string Name { get; set; }
        public string ProductName { get; set; }
        public int EditionNumber { get; set; }
        public int UpdateNumber { get; set; }
        public string ExchangeSetStandard { get; set; }
        public ETag ETag { get; set; }
    }
}
