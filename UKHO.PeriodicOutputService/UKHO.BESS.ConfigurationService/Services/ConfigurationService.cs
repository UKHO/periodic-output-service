using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.BESS;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<ConfigurationService> logger;
        public ConfigurationService(IAzureTableStorageHelper azureTableStorageHelper, ILogger<ConfigurationService> logger)
        {
            this.azureTableStorageHelper = azureTableStorageHelper;
            this.logger = logger;
        }

        public List<ConfigurationSetting> ProcessConfigs()
        {
            ////string content = File.ReadAllText(@"C:\home\site\wwwroot\App_Data\jobs\continuous\UKHO.BESS.ConfigurationService\TempConfigurationSetting.json");
            string content = File.ReadAllText(@"D:\Repos\Periodic-Output-Service\UKHO.PeriodicOutputService\UKHO.BESS.ConfigurationService\TempConfigurationSetting.json");
            return JsonConvert.DeserializeObject<List<ConfigurationSetting>>(content)!;
        }

        /// <summary>
        /// check cron expression and save bespoke details to msg queue
        /// </summary>
        /// <param name="configurationSettings"></param>
        /// <returns></returns>
        public bool ScheduleConfigDetails(List<ConfigurationSetting> configurationSettings)
        {
            try
            {
                logger.LogInformation(EventIds.BessScheduleConfigStarted.ToEventId(), "Schedule config details started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                foreach (var configurationSetting in configurationSettings)
                {
                    var fullUpdateCronSchedule = CrontabSchedule.Parse(configurationSetting.Frequency);

                    var nextFullUpdateOccurrence = fullUpdateCronSchedule.GetNextOccurrence(DateTime.UtcNow);
                    ScheduleDetails scheduleDetails = GetNextScheduledDateTime(nextFullUpdateOccurrence, configurationSetting);

                    var intervalInMins = ((int)scheduleDetails.ScheduleTime.Subtract(DateTime.UtcNow).TotalMinutes);
                    var isSameDay = scheduleDetails.ScheduleTime.Date.Subtract(DateTime.UtcNow.Date).Days == 0;

                    if (intervalInMins <= 0 && isSameDay && scheduleDetails.IsExecuted.Equals(false)) //Check if config schedule is missed or if it's due for the same day.
                    {
                        /* -- save details to msg queue --
                         * 
                         * 
                         * 
                         */

                        logger.LogInformation(EventIds.BessScheduleConfigRunning.ToEventId(), "Running schedule config for Name : {Name} | Frequency : {Frequency}| _X-Correlation-ID : {CorrelationId}", configurationSetting.Name, configurationSetting.Frequency, CommonHelper.CorrelationID);
                        azureTableStorageHelper.RefreshNextSchedule(nextFullUpdateOccurrence, configurationSetting, true);
                    }
                    else
                    {
                        if (scheduleDetails.ScheduleTime < nextFullUpdateOccurrence || scheduleDetails.IsEnabled != configurationSetting.IsEnabled)
                        {
                            azureTableStorageHelper.RefreshNextSchedule(nextFullUpdateOccurrence, configurationSetting, false);
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

        private ScheduleDetails GetNextScheduledDateTime(DateTime nextFullUpdateOccurrence, ConfigurationSetting configurationSetting)
        {
            ScheduleDetails scheduleDetails = azureTableStorageHelper.GetNextScheduleDetails(configurationSetting.Name);

            if (scheduleDetails == null)
            {
                azureTableStorageHelper.RefreshNextSchedule(nextFullUpdateOccurrence, configurationSetting, false);

                ScheduleDetails scheduleDetail = new();
                {
                    scheduleDetail.ScheduleTime = nextFullUpdateOccurrence;
                    scheduleDetail.IsEnabled = configurationSetting.IsEnabled;
                }

                return scheduleDetail;
            }
            return scheduleDetails;
        }
    }
}
