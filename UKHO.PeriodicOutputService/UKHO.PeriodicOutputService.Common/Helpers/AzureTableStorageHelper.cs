using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureTableStorageHelper : IAzureTableStorageHelper
    {
        private const string Webjob_Trigger_TABLE_NAME = "poswebjobtrigger";

        private readonly IOptions<AzureStorageConfiguration> _azureStorageConfig;

        public AzureTableStorageHelper(IOptions<AzureStorageConfiguration> azureStorageConfig)
        {
            _azureStorageConfig = azureStorageConfig;
        }

        public void SaveEntity(Task[] tasks, DateTime nextSchedule)
        {
            long invertedTimeKey = DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks;
            TableClient tableWebjobEntityClient = GetWebjobEntityClient(Webjob_Trigger_TABLE_NAME);
            WebjobEntity webJobEntity = new()
            {
                PartitionKey = DateTime.UtcNow.ToString("MMyyyy"),
                RowKey = invertedTimeKey.ToString(),
                IsJobSuccess = tasks.All(p => p.IsCompletedSuccessfully),
                SinceDateTime = nextSchedule
            };

            tableWebjobEntityClient.AddEntity(webJobEntity);
        }

        public DateTime GetSinceDateTime()
        {
            TableClient tableWebjobEntityClient = GetWebjobEntityClient(Webjob_Trigger_TABLE_NAME);
            WebjobEntity? latestrecord = tableWebjobEntityClient.Query<WebjobEntity>().OrderByDescending(p => p.Timestamp).FirstOrDefault();
            return latestrecord?.SinceDateTime ?? DateTime.UtcNow.AddDays(-7);
        }

        private TableClient GetWebjobEntityClient(string tableName)
        {
            var serviceClient = new TableServiceClient(_azureStorageConfig.Value.ConnectionString);
            TableClient? tableClient = serviceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        }

        private WebjobEntity GetWebjobEntity(TableClient tableClient)
        {
            WebjobEntity webJobEntity = new();
            //Response<WebjobEntity>? response = tableClient.GetEntity<WebjobEntity>(partitionKey, rowKey);

            webJobEntity.PartitionKey = DateTime.UtcNow.ToString("MM/yyyy");
            webJobEntity.RowKey = Guid.NewGuid().ToString();
            return webJobEntity;
        }
    }
}
