using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.BESS;

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
        public async Task<List<ConfigurationSetting>> ReadConfigurationJsonFiles()
        {
            await Task.CompletedTask;
            ////string content = File.ReadAllText(@"C:\home\site\wwwroot\App_Data\jobs\continuous\UKHO.BESS.ConfigurationService\TempConfigurationSetting.json");
            string content = File.ReadAllText(@"D:\Repos\Periodic-Output-Service\UKHO.PeriodicOutputService\UKHO.BESS.ConfigurationService\TempConfigurationSetting.json");
            return JsonConvert.DeserializeObject<List<ConfigurationSetting>>(content)!;
        }

        /// <summary>
        /// check cron expression and save bespoke details to msg queue
        /// </summary>
        /// <param name="configurationSettings"></param>
        /// <returns></returns>
        public async Task SaveBespokeDetailsToQueue(List<ConfigurationSetting> configurationSettings)
        {
            try
            {
                logger.LogInformation(EventIds.BESSSaveMsgQueueStarted.ToEventId(), "Save bespoke details to msg queue started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);

                //Get todays bess frequency details
                var bessFrequencyHistories = azureTableStorageHelper.GetBessFrequencyHistory();

                var configurationServiceFrequencyDetails = configurationSettings.Where(x => x.IsEnabled.Equals(true)
                                                                                        && GetScheduleTime(x.Frequency).Day.Equals(DateTime.UtcNow.Day) //Todays data                                                                                        
                                                                                        && !bessFrequencyHistories.Any(y => y.Name.Equals(x.Name))).ToList(); //Same day not executed yet data

                if (configurationServiceFrequencyDetails.Any())
                {
                    //Save Bess frequency data to history table
                    azureTableStorageHelper.SaveBessFrequencyDetails(configurationServiceFrequencyDetails);

                    logger.LogInformation(EventIds.BESSFrequencyDetailsAdded.ToEventId(), "Bespoke frequency added in table for Name : {Name} | {DateTime} | _X-Correlation-ID : {CorrelationId}", string.Join(",", configurationServiceFrequencyDetails.Select(i => i.Name)), DateTime.UtcNow, CommonHelper.CorrelationID);
                }

                //Get todays updated bess frequency details
                var bessLatestFrequencyHistories = azureTableStorageHelper.GetBessFrequencyHistory();

                foreach (var bessFrequencyHistory in bessLatestFrequencyHistories)
                {
                    if (((int)GetScheduleTime(bessFrequencyHistory.Frequency).Subtract(DateTime.UtcNow).TotalMinutes) <= 0)
                    {
                        //Add bespoke details to msg queue

                        logger.LogInformation(EventIds.BESSMsgQueueDetailsSaved.ToEventId(), "Bespoke details added in msg queue for Name : {Name} | {DateTime} | _X-Correlation-ID : {CorrelationId}", bessFrequencyHistory.Name, DateTime.UtcNow, CommonHelper.CorrelationID);
                    }
                }

                await Task.CompletedTask;

                logger.LogInformation(EventIds.BESSSaveMsgQueueCompleted.ToEventId(), "Save bespoke details to msg queue completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BESSSaveMsgQueueException.ToEventId(), "Save bespoke details to msg queue failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                throw;
            }
        }

        private DateTime GetScheduleTime(string frequency)
        {
            var updateSchedule = CrontabSchedule.Parse(frequency);
            return updateSchedule.GetNextOccurrence(DateTime.UtcNow);
        }
    }
}
