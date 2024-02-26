using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<ConfigurationService> logger;
        public ConfigurationService(IAzureTableStorageHelper azureTableStorageHelper, ILogger<ConfigurationService> logger)
        {
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<BessConfig> ProcessConfigs()
        {
            ////string content = File.ReadAllText(@"C:\home\site\wwwroot\App_Data\jobs\continuous\UKHO.BESS.ConfigurationService\TempConfigurationSetting.json");
            string content = File.ReadAllText(@"D:\Repos\Periodic-Output-Service\UKHO.PeriodicOutputService\UKHO.BESS.ConfigurationService\TempConfigurationSetting.json");
            return JsonConvert.DeserializeObject<List<BessConfig>>(content)!;
        }

        /// <summary>
        /// check cron expression and save bespoke details to msg queue
        /// </summary>
        /// <param name="configDetails"></param>
        /// <returns></returns>
        public bool ScheduleConfigDetails(List<BessConfig> configDetails)
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
