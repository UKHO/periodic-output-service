﻿using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
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
        private const string BESS_SCHEDULE_DETAILS_TABLE_NAME = "bessconfigscheduledetails";
        private const string BESS_PRODUCT_VERSION_DETAILS_TABLE_NAME = "bessproductversiondetails";

        private readonly IOptions<AzureStorageConfiguration> _azureStorageConfig;
        private readonly IConfiguration _configuration;

        public AzureTableStorageHelper(IOptions<AzureStorageConfiguration> azureStorageConfig, IConfiguration configuration)
        {
            _azureStorageConfig = azureStorageConfig;
            _configuration = configuration;
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
            TableClient productVersionDetailsEntityClient = GetTableClient(_azureStorageConfig.Value.AioProductVersionDetailsTableName);

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
            TableClient productVersionDetailsEntityClient = GetTableClient(_azureStorageConfig.Value.AioProductVersionDetailsTableName);
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

        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            var serviceClient = new TableServiceClient(_azureStorageConfig.Value.ConnectionString);
            TableClient tableClient = serviceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        public async Task UpsertScheduleDetailAsync(DateTime nextSchedule, BessConfig bessConfig, bool isExecuted)
        {
            ScheduleDetailEntity scheduleDetailEntity = new()
            {
                PartitionKey = "BessConfigSchedule",
                RowKey = bessConfig.Name,
                NextScheduleTime = nextSchedule,
                IsEnabled = bessConfig.IsEnabled,
                IsExecuted = isExecuted
            };

            TableClient tableJobScheduleEntityClient = await GetTableClientAsync(BESS_SCHEDULE_DETAILS_TABLE_NAME);
           await tableJobScheduleEntityClient.UpsertEntityAsync(scheduleDetailEntity);
        }

        public async Task<ScheduleDetailEntity> GetScheduleDetailAsync(string configName)
        {
            TableClient tableJobScheduleEntityClient = await GetTableClientAsync(BESS_SCHEDULE_DETAILS_TABLE_NAME);
            ScheduleDetailEntity scheduleDetailEntity = tableJobScheduleEntityClient.Query<ScheduleDetailEntity>().FirstOrDefault(i => i.IsEnabled.ToLower().Equals("yes") && i.IsExecuted.Equals(false) && i.RowKey.Equals(configName));
            return scheduleDetailEntity;
        }

        public async Task<List<ProductVersionEntities>> GetLatestBessProductVersionDetailsAsync()
        {
            TableClient tableClient = await GetTableClientAsync(BESS_PRODUCT_VERSION_DETAILS_TABLE_NAME);
            List<ProductVersionEntities> bessProductVersionEntities = tableClient.Query<ProductVersionEntities>().ToList();

            return bessProductVersionEntities;
        }

        public async Task SaveBessProductVersionDetailsAsync(List<ProductVersion> bessProductVersions, string name, string exchangeSetStandard)
        {
            TableClient tableClient = await GetTableClientAsync(BESS_PRODUCT_VERSION_DETAILS_TABLE_NAME);

            foreach (var item in bessProductVersions)
            {
                ProductVersionEntities bessProductVersionEntities = new();

                bessProductVersionEntities = new()
                {
                    PartitionKey = name,
                    RowKey = exchangeSetStandard + "|" + item.ProductName,
                    EditionNumber = item.EditionNumber,
                    UpdateNumber = item.UpdateNumber
                };
                await tableClient.UpsertEntityAsync(bessProductVersionEntities);
            }
        }

        public AioJobConfigurationEntities? GetAioJobConfiguration()
        {
            string? jobId = _configuration["AioJobId"];

            if (string.IsNullOrEmpty(jobId)) return null;

            TableClient tableClient = GetTableClient(_azureStorageConfig.Value.AioJobConfigurationTableName);
            AioJobConfigurationEntities aioJobConfigurationEntities = tableClient.Query<AioJobConfigurationEntities>().FirstOrDefault(i => i.RowKey.Equals(jobId) && i.IsEnabled);
            return aioJobConfigurationEntities;
        }
    }
}
