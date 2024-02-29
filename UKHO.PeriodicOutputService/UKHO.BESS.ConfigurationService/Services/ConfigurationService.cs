using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, IAzureTableStorageHelper azureTableStorageHelper, ILogger<ConfigurationService> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                        CheckConfigFrequencyAndSaveQueueDetails(bessConfigs);
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
        /// check cron expression and save bespoke details to msg queue
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

                    var intervalInMins = ((int)existingScheduleDetail.NextScheduleTime.Subtract(DateTime.UtcNow).TotalMinutes);
                    var isSameDay = existingScheduleDetail.NextScheduleTime.Date.Subtract(DateTime.UtcNow.Date).Days == 0;

                    if (CheckSchedule(intervalInMins, isSameDay, config, existingScheduleDetail)) //Check if config schedule is missed or if it's due for the same day.
                    {
                        /* -- save details to msg queue --
                         *
                         *
                         *
                         */

                        logger.LogInformation(EventIds.BessConfigFrequencyElapsed.ToEventId(), "Config for Name : {Name} | Frequency : {Frequency} | executed at Timestamp: {Timestamp} | _X-Correlation-ID : {CorrelationId}", config.Name, config.Frequency, DateTime.UtcNow, CommonHelper.CorrelationID);
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
                logger.LogError(EventIds.BessScheduleConfigException.ToEventId(), "Exception at schedule config details {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                return false;
            }
        }

        [ExcludeFromCodeCoverage]
        private static bool IsScheduleRefreshed(ScheduleDetailEntity scheduleDetail, DateTime nextFullUpdateOccurrence, BessConfig bessConfig) => scheduleDetail.NextScheduleTime != nextFullUpdateOccurrence || scheduleDetail.IsEnabled != bessConfig.IsEnabled;

        [ExcludeFromCodeCoverage]
        private static bool CheckSchedule(int intervalInMins, bool isSameDay, BessConfig bessConfig, ScheduleDetailEntity scheduleDetail) => intervalInMins <= 0 && isSameDay && bessConfig.IsEnabled.Equals(true) && scheduleDetail.IsExecuted.Equals(false);

        [ExcludeFromCodeCoverage]
        private ScheduleDetailEntity GetScheduleDetail(DateTime nextOccurrence, BessConfig bessConfig)
        {
            var existingScheduleDetail = azureTableStorageHelper.GetScheduleDetail(bessConfig.Name);

            if (existingScheduleDetail == null)
            {
                azureTableStorageHelper.UpsertScheduleDetail(nextOccurrence, bessConfig, false);

                ScheduleDetailEntity scheduleDetail = new();
                {
                    scheduleDetail.NextScheduleTime = nextOccurrence;
                    scheduleDetail.IsEnabled = bessConfig.IsEnabled;
                }

                return scheduleDetail;
            }
            return existingScheduleDetail;
        }
    }
}
