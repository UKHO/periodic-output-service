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

        public IList<BessConfig> ProcessConfigs()
        {
            IList<BessConfig> bessConfigs = new List<BessConfig>();

            try
            {
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
                }
                else
                {
                    logger.LogWarning(EventIds.BessConfigsNotFound.ToEventId(), "Bess configs not found | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                }

                logger.LogInformation(EventIds.BessConfigsProcessingCompleted.ToEventId(), "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                return bessConfigs;

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
        /// <param name="configDetails"></param>
        /// <returns></returns>
        public bool ScheduleConfigDetails(IList<BessConfig> configDetails)
        {
            try
            {
                logger.LogInformation(EventIds.BessScheduleConfigStarted.ToEventId(), "Schedule config details started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                foreach (var configDetail in configDetails)
                {
                    var fullUpdateCronSchedule = CrontabSchedule.Parse(configDetail.Frequency);

                    var nextFullUpdateOccurrence = fullUpdateCronSchedule.GetNextOccurrence(DateTime.UtcNow);
                    ScheduleDetails scheduleDetails = GetNextSchedule(nextFullUpdateOccurrence, configDetail);

                    var intervalInMins = ((int)scheduleDetails.NextScheduleTime.Subtract(DateTime.UtcNow).TotalMinutes);
                    var isSameDay = scheduleDetails.NextScheduleTime.Date.Subtract(DateTime.UtcNow.Date).Days == 0;
                    bool isMissed = CheckScheduleTime(intervalInMins, isSameDay, configDetail, scheduleDetails);
                    if (isMissed) //Check if config schedule is missed or if it's due for the same day.
                    {
                        /* -- save details to msg queue --
                         * 
                         * 
                         * 
                         */

                        logger.LogInformation(EventIds.BessScheduleConfigRunning.ToEventId(), "Running schedule config for Name : {Name} | Frequency : {Frequency}| _X-Correlation-ID : {CorrelationId}", configDetail.Name, configDetail.Frequency, CommonHelper.CorrelationID);
                        azureTableStorageHelper.RefreshNextSchedule(nextFullUpdateOccurrence, configDetail, true);
                    }
                    else
                    {       //Update schedule details
                        bool isNextSchedule = IsNextSchedule(scheduleDetails, nextFullUpdateOccurrence, configDetail);
                        if (isNextSchedule)
                        {
                            azureTableStorageHelper.RefreshNextSchedule(nextFullUpdateOccurrence, configDetail, false);
                        }
                    }
                }
                logger.LogInformation(EventIds.BessScheduleConfigCompleted.ToEventId(), "Schedule config details completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessScheduleConfigException.ToEventId(), "Exception at schedule config details {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                return false;
            }
        }
        [ExcludeFromCodeCoverage]
        private static bool IsNextSchedule(ScheduleDetails scheduleDetails, DateTime nextFullUpdateOccurrence, BessConfig configDetail) => scheduleDetails.NextScheduleTime != nextFullUpdateOccurrence || scheduleDetails.IsEnabled != configDetail.IsEnabled;

        [ExcludeFromCodeCoverage]
        private static bool CheckScheduleTime(int intervalInMins, bool isSameDay, BessConfig configDetail, ScheduleDetails scheduleDetails) => intervalInMins <= 0 && isSameDay && configDetail.IsEnabled && scheduleDetails.IsExecuted.Equals(false);

        [ExcludeFromCodeCoverage]
        private ScheduleDetails GetNextSchedule(DateTime nextFullUpdateOccurrence, BessConfig configDetails)
        {
            ScheduleDetails scheduleDetails = azureTableStorageHelper.GetNextScheduleDetails(configDetails.Name);

            if (scheduleDetails == null)
            {
                azureTableStorageHelper.RefreshNextSchedule(nextFullUpdateOccurrence, configDetails, false);

                ScheduleDetails scheduleDetail = new();
                {
                    scheduleDetail.NextScheduleTime = nextFullUpdateOccurrence;
                    scheduleDetail.IsEnabled = configDetails.IsEnabled;
                }

                return scheduleDetail;
            }
            return scheduleDetails;
        }
    }
}
