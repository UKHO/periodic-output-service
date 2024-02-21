using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureTableStorageHelper : IAzureTableStorageHelper
    {
        private const string WEBJOB_HISTORY_TABLE_NAME = "poswebjobhistory";
        private const string AIO_PRODUCT_VERSION_DETAILS_TABLE_NAME = "aioproductversiondetails";
        private const string BESS_SCHEDULE_DETAILS_TABLE_NAME = "bessconfigscheduledetails";

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

        public void SaveProductVersionDetails(List<ProductVersion> productVersions)
        {
            TableClient productVersionDetailsEntityClient = GetTableClient(AIO_PRODUCT_VERSION_DETAILS_TABLE_NAME);

            foreach (var item in productVersions)
            {
                ProductVersionEntities productVersionEntities = productVersionDetailsEntityClient
                                                                    .Query<ProductVersionEntities>()
                                                                    .FirstOrDefault(p => p.ProductName == item.ProductName &&
                                                                        p.EditionNumber == item.EditionNumber);

                if (productVersionEntities == null)
                {
                    long invertedTimeKey = DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks;

                    productVersionEntities = new();
                    productVersionEntities.PartitionKey = DateTime.UtcNow.ToString("MMyyyy");
                    productVersionEntities.RowKey = invertedTimeKey.ToString();
                }

                productVersionEntities.ProductName = item.ProductName;
                productVersionEntities.EditionNumber = item.EditionNumber;
                productVersionEntities.UpdateNumber = item.UpdateNumber;
                productVersionDetailsEntityClient.UpsertEntity(productVersionEntities);
            }
        }

        public List<ProductVersionEntities> GetLatestProductVersionDetails()
        {
            TableClient productVersionDetailsEntityClient = GetTableClient(AIO_PRODUCT_VERSION_DETAILS_TABLE_NAME);
            List<ProductVersionEntities> productVersionEntities = productVersionDetailsEntityClient.Query<ProductVersionEntities>().ToList();

            return productVersionEntities;
        }

        private TableClient GetTableClient(string tableName)
        {
            var serviceClient = new TableServiceClient(_azureStorageConfig.Value.ConnectionString);
            TableClient? tableClient = serviceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        }

        public void RefreshNextSchedule(DateTime nextSchedule, BessConfig configDetails)
        {
            ScheduleDetails scheduleDetails = new()
            {
                PartitionKey = "BessConfigSchedule",
                RowKey = configDetails.Name,
                NextScheduleTime = nextSchedule,
                IsEnabled = configDetails.IsEnabled,
            };

            TableClient tableJobScheduleEntityClient = GetTableClient(BESS_SCHEDULE_DETAILS_TABLE_NAME);
            tableJobScheduleEntityClient.UpsertEntity(scheduleDetails);
        }

        public ScheduleDetails GetNextScheduleDetails(string name)
        {
            TableClient tableJobScheduleEntityClient = GetTableClient(BESS_SCHEDULE_DETAILS_TABLE_NAME);
            ScheduleDetails scheduleDetail = tableJobScheduleEntityClient.Query<ScheduleDetails>().Where(i => i.IsEnabled.Equals(true) && i.RowKey.Equals(name)
                                                                                                            && i.Timestamp!.Value.Date.Equals(DateTime.UtcNow.Date)).FirstOrDefault();
            return scheduleDetail;
        }
    }
}
