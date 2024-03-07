using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<ConfigurationService> logger;
        private readonly IConfiguration configuration;
        private const string UndefinedValue = "undefined";

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, IAzureTableStorageHelper azureTableStorageHelper, ILogger<ConfigurationService> logger, IConfiguration configuration)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void ProcessConfigs()
        {
            try
            {
                IList<BessConfig> bessConfigs = new List<BessConfig>();

                Dictionary<string, string> configs = azureBlobStorageClient.GetConfigsInContainer();

                logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}", configs.Keys.Count, CommonHelper.CorrelationID);

                if (configs.Any())
                {
                    foreach (string fileName in configs.Keys.ToList())
                    {
                        string content = configs[fileName];

                        bool isValidJson = IsValidJson(content, fileName);

                        if (isValidJson)
                        {
                            IList<BessConfig> bessConfig = JsonConvert.DeserializeObject<List<BessConfig>>(content)!;

                            foreach (BessConfig config in bessConfig)
                            {
                                bessConfigs.Add(config);
                            }
                        }
                    }

                    if (bessConfigs.Any())
                    {
                        //Temp scs call
                        List<SalesCatalogueDataProductResponse> salesCatalogueDataProducts = new();

                        CheckConfigFrequencyAndSaveQueueDetails(bessConfigs, salesCatalogueDataProducts);
                    }
                }
                else
                {
                    logger.LogWarning(EventIds.BessConfigsNotFound.ToEventId(), "Bess configs not found | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                }

                logger.LogInformation(EventIds.BessConfigsProcessingCompleted.ToEventId(), "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigsProcessingFailed.ToEventId(), "Bess configs Processing failed with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }

        private bool IsValidJson(string json, string fileName)
        {
            try
            {
                var token = JToken.Parse(json);

                if (!token.ToString().Contains(UndefinedValue))
                {
                    return true;
                }

                logger.LogWarning(EventIds.BessConfigIsInvalid.ToEventId(), "Bess config is invalid for file : {fileName} | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigParsingError.ToEventId(), "Error occurred while parsing Bess config file:{fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                return false;
            }
        }

        /// <summary>
        /// Check config frequency and save details to message queue
        /// </summary>
        /// <param name="bessConfigs"></param>
        /// <param name="salesCatalogueDataProducts"></param>
        /// <returns></returns>
        public bool CheckConfigFrequencyAndSaveQueueDetails(IList<BessConfig> bessConfigs, IList<SalesCatalogueDataProductResponse> salesCatalogueDataProducts)
        {
            try
            {
                foreach (var config in bessConfigs)
                {
                    // Parse the cron expression using NCronTab library
                    var schedule = CrontabSchedule.Parse(config.Frequency);

                    // Get the next occurrence of the cron expression after the last execution time
                    var nextOccurrence = schedule.GetNextOccurrence(DateTime.UtcNow);
                    ScheduleDetailEntity existingScheduleDetail = GetScheduleDetail(nextOccurrence, config);

                    if (CheckSchedule(config, existingScheduleDetail)) //Check if config schedule is missed or if it's due for the same day.
                    {
                        logger.LogInformation(EventIds.BessConfigFrequencyElapsed.ToEventId(), "Bess Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}", config.Name, config.Frequency, existingScheduleDetail.NextScheduleTime, DateTime.UtcNow, CommonHelper.CorrelationID);

                        var encCells = GetEncCells(config.EncCellNames, salesCatalogueDataProducts);

                        if (!encCells.Any()) //If cells are not found then bespoke exchange set will not create
                        {
                            logger.LogWarning(EventIds.BessInvalidCellAndPatternNotFoundInCatalog.ToEventId(), "All listed cells are not found and neither cell matching with the pattern, bespoke will not create for : {EncCellName} | _X-Correlation-ID : {CorrelationId}", string.Join(", ", config.EncCellNames), CommonHelper.CorrelationID);
                            continue;
                        }

                        int? totalFileSize = encCells.Select(i => i.Item2).Sum();

                        /* -- save details to message queue --
                         *
                         *
                         *
                         */

                        azureTableStorageHelper.UpsertScheduleDetail(nextOccurrence, config, true);
                    }
                    else
                    {   //Update schedule details
                        if (IsScheduleRefreshed(existingScheduleDetail, nextOccurrence, config))
                        {
                            azureTableStorageHelper.UpsertScheduleDetail(nextOccurrence, config, false);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigFrequencyProcessingException.ToEventId(), "Exception occurred while processing Bess config {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                return false;
            }
        }

        [ExcludeFromCodeCoverage]
        private static bool IsScheduleRefreshed(ScheduleDetailEntity scheduleDetailEntity, DateTime nextOccurrence, BessConfig bessConfig) => scheduleDetailEntity.NextScheduleTime != nextOccurrence || scheduleDetailEntity.IsEnabled != bessConfig.IsEnabled;

        [ExcludeFromCodeCoverage]
        private static bool CheckSchedule(BessConfig bessConfig, ScheduleDetailEntity scheduleDetailEntity)
        {
            var intervalInMinutes = ((int)scheduleDetailEntity.NextScheduleTime.Subtract(DateTime.UtcNow).TotalSeconds);
            var isSameDay = scheduleDetailEntity.NextScheduleTime.Date.Subtract(DateTime.UtcNow.Date).Days == 0;

            return intervalInMinutes <= 0 && isSameDay && bessConfig.IsEnabled.ToLower().Equals("yes");
        }

        [ExcludeFromCodeCoverage]
        private ScheduleDetailEntity GetScheduleDetail(DateTime nextOccurrence, BessConfig bessConfig)
        {
            var existingScheduleDetail = azureTableStorageHelper.GetScheduleDetail(bessConfig.Name);

            if (existingScheduleDetail != null)
            {
                return existingScheduleDetail;
            }

            azureTableStorageHelper.UpsertScheduleDetail(nextOccurrence, bessConfig, false);

            ScheduleDetailEntity scheduleDetailEntity = new();
            {
                scheduleDetailEntity.NextScheduleTime = nextOccurrence;
                scheduleDetailEntity.IsEnabled = bessConfig.IsEnabled;
            }

            return scheduleDetailEntity;
        }

        /// <summary>
        /// Fetched the list of all the ENC cell names from the provided prefix patterns
        /// AIO cell is excluded
        /// </summary>
        /// <param name="encCellNames"></param>
        /// <param name="salesCatalogueProducts"></param>
        /// <returns></returns>
        private IEnumerable<(string, int?)> GetEncCells(IEnumerable<string> encCellNames, IEnumerable<SalesCatalogueDataProductResponse> salesCatalogueProducts)
        {
            #region filter provided prefix patterns

            List<string> ignoreList = new();
            IEnumerable<string> encCells = encCellNames.Where(i => i.EndsWith("*"));

            foreach (string encCell in encCells)
            {
                ignoreList.AddRange(encCellNames.Where(x => x.StartsWith(encCell.Remove(encCell.Length - 1)) && !x.Equals(encCell)));
            }

            IEnumerable<string> prefixPatterns = encCellNames.Where(y => !ignoreList.Any(z => z.Equals(y)));

            #endregion

            #region get enc cell from provided prefix patterns

            List<(string, int?)> filteredEncCell = new();
            List<string> invalidPatternOrCell = new();

            foreach (var prefixPattern in prefixPatterns)
            {
                IEnumerable<SalesCatalogueDataProductResponse> salesCatalogueDataProducts = prefixPattern.EndsWith('*') ? salesCatalogueProducts.Where(x => x.ProductName.StartsWith(prefixPattern.Remove(prefixPattern.Length - 1)))
                                                                                                                        : salesCatalogueProducts.Where(x => x.ProductName.Equals(prefixPattern));

                if (salesCatalogueDataProducts.Any())
                {
                    filteredEncCell.AddRange(salesCatalogueDataProducts.Select(t => (t.ProductName, t.FileSize)));
                }
                else //add invalid cells
                {
                    invalidPatternOrCell.Add(prefixPattern);
                }
            }
            if (invalidPatternOrCell.Any() && filteredEncCell.Any()) //If invalid pattern found then log
            {
                logger.LogWarning(EventIds.BessInvalidEncCellOrPatternNotFoundInCatalog.ToEventId(), "Invalid pattern or cell found : {InvalidEncCellName} | EncCellNames : {EncCellName} | _X-Correlation-ID : {CorrelationId}", string.Join(", ", invalidPatternOrCell), string.Join(", ", encCellNames), CommonHelper.CorrelationID);
            }

            return filteredEncCell.Where(x => !configuration["AioCells"].Split(",").Any(i => i.Equals(x.Item1))); //remove aio cells and return all filtered data

            #endregion
        }
    }
}
