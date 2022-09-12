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
        private const string WEBJOB_HISTORY_TABLE_NAME = "poswebjobhistory";

        private readonly IOptions<AzureStorageConfiguration> _azureStorageConfig;

        public AzureTableStorageHelper(IOptions<AzureStorageConfiguration> azureStorageConfig)
        {
            _azureStorageConfig = azureStorageConfig;
        }

        public void SaveHistory(WebJobHistory webJobHistory)
        {
            TableClient tableWebJobEntityClient = GetTableClient(WEBJOB_HISTORY_TABLE_NAME);
            tableWebJobEntityClient.AddEntity(webJobHistory);
        }

        public DateTime GetSinceDateTime()
        {
            TableClient tableWebjobEntityClient = GetTableClient(WEBJOB_HISTORY_TABLE_NAME);
            WebJobHistory? latestrecord = tableWebjobEntityClient.Query<WebJobHistory>().OrderByDescending(p => p.Timestamp).FirstOrDefault();
            return latestrecord?.SinceDateTime ?? DateTime.UtcNow.AddDays(-7);
        }

        private TableClient GetTableClient(string tableName)
        {
            var serviceClient = new TableServiceClient(_azureStorageConfig.Value.ConnectionString);
            TableClient? tableClient = serviceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        }
    }
}
