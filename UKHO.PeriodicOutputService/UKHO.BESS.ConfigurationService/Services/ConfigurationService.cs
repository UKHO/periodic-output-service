using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<ConfigurationService> logger;
        private readonly ISalesCatalogueService salesCatalogueService;
        private readonly IConfiguration configuration;
        private const string UndefinedValue = "undefined";
        private readonly IConfigValidator configValidator;
        private readonly IAzureBlobStorageService azureBlobStorageService;
        private List<string> invalidNameList = new();
        private int configsWithUndefinedValueCount;
        private int configsWithDuplicateNameAttributeCount;
        private const string NewLine = "\n";
        private const string Colon = ": ";
        private const string Hyphen = "- ";

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient,
                                    IAzureTableStorageHelper azureTableStorageHelper,
                                    ILogger<ConfigurationService> logger,
                                    IConfigValidator configValidator,
                                    ISalesCatalogueService salesCatalogueService,
                                    IConfiguration configuration,
                                    IAzureBlobStorageService azureBlobStorageService)

        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.azureBlobStorageService = azureBlobStorageService ?? throw new ArgumentNullException(nameof(azureBlobStorageService));
        }

        public void ProcessConfigs()
        {
            try
            {
                var configsInContainer = azureBlobStorageClient.GetConfigsInContainer();
                if (!configsInContainer.Any())
                {
                    logger.LogWarning(EventIds.BessConfigsNotFound.ToEventId(), "Bess configs not found | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                    return;
                }

                logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}", configsInContainer.Keys.Count, CommonHelper.CorrelationID);

                IList<BessConfig> bessConfigs = new List<BessConfig>();

                var salesCatalogueDataResponse = Task.Run(async () => await salesCatalogueService.GetSalesCatalogueData()).Result;

                int configsWithInvalidAttributeCount = 0, deserializedConfigsCount = 0;

                foreach (string fileName in configsInContainer.Keys.ToList())
                {
                    string content = configsInContainer[fileName];

                    var deserializedConfigs = DeserializeConfig(content, fileName);

                    foreach (BessConfig deserializedConfig in deserializedConfigs)
                    {
                        deserializedConfigsCount = deserializedConfigsCount + 1;
                        deserializedConfig.FileName = fileName; //for logging
                        try
                        {
                            ValidationResult results = configValidator.Validate(deserializedConfig);

                            if (!results.IsValid)
                            {
                                configsWithInvalidAttributeCount = configsWithInvalidAttributeCount + 1;

                                var errors = new StringBuilder();

                                foreach (var failure in results.Errors)
                                {
                                    errors.AppendLine(NewLine + failure.PropertyName + Colon + failure.ErrorMessage);
                                }

                                invalidNameList.Add(deserializedConfig.FileName + Hyphen + deserializedConfig.Name);

                                logger.LogError(EventIds.BessConfigInvalidAttributes.ToEventId(), "Bess Config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}", fileName, errors, CommonHelper.CorrelationID);
                            }
                            else
                            {
                                bessConfigs.Add(deserializedConfig);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(EventIds.BessConfigValidationError.ToEventId(), "Error occurred while validating Bess config file : {fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                        }
                    }
                }

                RemoveDuplicateConfigs((List<BessConfig>)bessConfigs);

                int totalConfigCount = deserializedConfigsCount + configsWithUndefinedValueCount;
                logger.LogInformation(EventIds.BessConfigValidationSummary.ToEventId(),
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with undefined value : {filesWithUndefinedValueCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | _X-Correlation-ID : {CorrelationId}", totalConfigCount, bessConfigs.Count, configsWithInvalidAttributeCount, configsWithUndefinedValueCount, configsWithDuplicateNameAttributeCount, CommonHelper.CorrelationID);

                if (bessConfigs.Any())
                {
                    CheckConfigFrequencyAndSaveQueueDetails(bessConfigs, salesCatalogueDataResponse.ResponseBody);
                }

                logger.LogInformation(EventIds.BessConfigsProcessingCompleted.ToEventId(), "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigsProcessingFailed.ToEventId(), "Bess configs processing failed with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }

        private IList<BessConfig> DeserializeConfig(string json, string fileName)
        {
            IList<BessConfig> bessConfig = new List<BessConfig>();
            try
            {
                var token = JToken.Parse(json);

                if (token.ToString().Contains(UndefinedValue))
                {
                    configsWithUndefinedValueCount = configsWithUndefinedValueCount + 1;
                    logger.LogWarning(EventIds.BessConfigIsInvalid.ToEventId(), "Bess config file : {fileName} contains undefined values or is invalid | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
                }
                else
                {
                    bessConfig = JsonConvert.DeserializeObject<List<BessConfig>>(json)!;
                }
                return bessConfig;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigParsingError.ToEventId(), "Error occurred while parsing Bess config file : {fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                return bessConfig;
            }
        }

        private void RemoveDuplicateConfigs(List<BessConfig> bessConfigs)
        {
            //find duplicates with property Name
            var duplicateRecords = bessConfigs.GroupBy(x => new { x.Name })
                .Where(x => x.Skip(1).Any()).ToList();

            if (duplicateRecords.Any())
            {
                configsWithDuplicateNameAttributeCount = duplicateRecords.Select(record => record.Count()).Sum();

                foreach (var duplicateRecord in duplicateRecords)
                {
                    foreach (BessConfig? duplicateConfig in duplicateRecord.ToList())
                    {
                        logger.LogWarning(EventIds.BessConfigsDuplicateRecordsFound.ToEventId(),
                            "Bess config file : {fileName} found with duplicate Name attribute : {name} | _X-Correlation-ID : {CorrelationId}",
                            duplicateConfig.FileName, duplicateConfig.Name, CommonHelper.CorrelationID);

                        bessConfigs.RemoveAll(x =>
                            x.FileName.Equals(duplicateConfig.FileName, StringComparison.OrdinalIgnoreCase));
                    }
                }
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
                        logger.LogInformation(EventIds.BessConfigFrequencyElapsed.ToEventId(), "Bess Config file: {FileName}, Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}", config.FileName, config.Name, config.Frequency, existingScheduleDetail.NextScheduleTime, DateTime.UtcNow, CommonHelper.CorrelationID);

                        azureTableStorageHelper.UpsertScheduleDetail(nextOccurrence, config, true);

                        var encCells = GetEncCells(config.EncCellNames, salesCatalogueDataProducts);

                        if (!encCells.Any()) //If cells are not found then bespoke exchange set will not create
                        {
                            logger.LogWarning(EventIds.BessEncCellNamesAndPatternNotFoundInSalesCatalogue.ToEventId(), "Neither listed ENC cell names found nor the pattern matched for any cell, Bespoke Exchange Set will not be created for file:{FileName} with ENC cells {EncCellNames} | _X-Correlation-ID : {CorrelationId}", config.FileName, string.Join(", ", config.EncCellNames), CommonHelper.CorrelationID);
                            continue;
                        }

                        int? totalFileSize = encCells.Select(i => i.Item2).Sum();

                        double fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(totalFileSize!.Value);

                        int BESSize = Convert.ToInt16(configuration["BESSizeInMB"]);

                        if (fileSizeInMb > BESSize)
                        {
                            logger.LogWarning(EventIds.BessSizeExceedsThreshold.ToEventId(), "ES size {fileSizeInMb}MB which is more than threshold :{BESSize}MB, Bespoke Exchange Set will not be created for file:{FileName} | _X-Correlation-ID : {CorrelationId}", Math.Round(fileSizeInMb, 2), BESSize, config.FileName, CommonHelper.CorrelationID);

                            continue;
                        }
                        //--save details to message queue --

                        IEnumerable<string> encCellNames = encCells.Select(i => i.Item1).ToList();

                        azureBlobStorageService.SetConfigQueueMessageModelAndAddToQueue(config, encCellNames, totalFileSize);
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
                logger.LogError(EventIds.BessConfigFrequencyProcessingException.ToEventId(), "Exception occurred while processing Bess config {DateTime} | {ErrorMessage} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
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
            const string Pattern = "*";

            List<string> ignoreList = new();
            IEnumerable<string> encCells = encCellNames.Where(i => i.EndsWith(Pattern));

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
                IEnumerable<SalesCatalogueDataProductResponse> salesCatalogueDataProducts = prefixPattern.EndsWith(Pattern) ? salesCatalogueProducts.Where(x => x.ProductName.StartsWith(prefixPattern.Remove(prefixPattern.Length - 1)))
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
            //Apart from valid, invalid pattern or cell found then log
            if (invalidPatternOrCell.Any() && filteredEncCell.Any())
            {
                logger.LogWarning(EventIds.BessInvalidEncCellNamesOrPatternNotFoundInSalesCatalogue.ToEventId(), "Invalid pattern or ENC cell names found : {InvalidEncCellName} | _X-Correlation-ID : {CorrelationId}", string.Join(", ", invalidPatternOrCell), CommonHelper.CorrelationID);
            }

            return filteredEncCell.Where(x => !configuration["AioCells"].Split(",").Any(i => i.Equals(x.Item1))); //remove aio cells and return all filtered data

            #endregion
        }
    }
}
