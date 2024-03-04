using System.Diagnostics.CodeAnalysis;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<ConfigurationService> logger;
        private const string UndefinedValue = "undefined";
        private readonly IConfigValidator configValidator;
        private List<string> invalidNameList = new();

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, IAzureTableStorageHelper azureTableStorageHelper, ILogger<ConfigurationService> logger, IConfigValidator configValidator)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator)); ;
        }

        public void ProcessConfigs()
        {
            try
            {
                var configsInContainer = azureBlobStorageClient.GetConfigsInContainer();
                if (!configsInContainer.Any())
                {
                    logger.LogWarning(EventIds.BessConfigsNotFound.ToEventId(), "Bess configs not found | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                }

                logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}", configsInContainer.Keys.Count, CommonHelper.CorrelationID);

                IList<BessConfig> bessConfigs = new List<BessConfig>();

                int filesWithInvalidAttributeCount = 0;
                int totalConfigCount = 0;

                foreach (string fileName in configsInContainer.Keys.ToList())
                {
                    string content = configsInContainer[fileName];

                    var deserializedConfigs = DeserializeConfig(content, fileName);

                    foreach (BessConfig deserializedConfig in deserializedConfigs)
                    {
                        totalConfigCount++;
                        deserializedConfig.FileName = fileName; //for logging

                        ValidationResult results = configValidator.Validate(deserializedConfig);

                        if (!results.IsValid)
                        {
                            filesWithInvalidAttributeCount++;
                            string errors = string.Empty;

                            foreach (var failure in results.Errors)
                            {
                                errors += "\n" + failure.PropertyName + ": " + failure.ErrorMessage;
                            }

                            invalidNameList.Add(deserializedConfig.FileName + "- " + deserializedConfig.Name);

                            logger.LogError(EventIds.BessConfigInvalidAttributes.ToEventId(), "Bess Config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}", fileName, errors, CommonHelper.CorrelationID);
                        }
                        else
                        {
                            bessConfigs.Add(deserializedConfig);
                        }
                    }
                }

                RemoveDuplicateConfigs((List<BessConfig>)bessConfigs);

                logger.LogInformation(EventIds.BessConfigValidationSummary.ToEventId(),
"Configs validation summary, valid configs : {validFileCount} | invalid configs with missing attributes or values : {invalidFileCount} | _X-Correlation-ID : {CorrelationId}", bessConfigs.Count, filesWithInvalidAttributeCount, CommonHelper.CorrelationID);

                if (bessConfigs.Any())
                {
                    CheckConfigFrequencyAndSaveQueueDetails(bessConfigs);
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

            if (!duplicateRecords.Any()) return;

            int duplicateFileCount = duplicateRecords.Select(record => record.Count()).Sum();

            foreach (var duplicateRecord in duplicateRecords)
            {
                foreach (BessConfig? duplicateConfig in duplicateRecord.ToList())
                {
                    logger.LogWarning(EventIds.BessConfigsDuplicateRecordsFound.ToEventId(), "Bess config file : {fileName} found with Dupicate Name attribute : {name} | _X-Correlation-ID : {CorrelationId}", duplicateConfig.FileName, duplicateConfig.Name, CommonHelper.CorrelationID);

                    bessConfigs.RemoveAll(x =>
                        x.FileName.Equals(duplicateConfig.FileName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        /// <summary>
        /// Check config frequency and save details to message queue
        /// </summary>
        /// <param name="bessConfigs"></param>
        /// <returns></returns>
        public bool CheckConfigFrequencyAndSaveQueueDetails(IList<BessConfig> bessConfigs)
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
                        /* -- save details to message queue --
                         *
                         *
                         *
                         */

                        logger.LogInformation(EventIds.BessConfigFrequencyElapsed.ToEventId(), "Bess Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}", config.Name, config.Frequency, existingScheduleDetail.NextScheduleTime, DateTime.UtcNow, CommonHelper.CorrelationID);
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
    }
}
