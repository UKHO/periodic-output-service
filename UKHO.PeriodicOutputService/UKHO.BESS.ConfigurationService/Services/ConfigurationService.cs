using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.BESS;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly List<ConfigurationSetting> configSettings = new();
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<ConfigurationService> logger;

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, ILogger<ConfigurationService> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ConfigurationSetting>> ReadConfigurationJsonFiles()
        {
            try
            {
                logger.LogInformation(EventIds.BESSJsonFileProcessingStarted.ToEventId(), "Json file processing started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                List<string> jsonContent = await azureBlobStorageClient.GetJsonStringListFromBlobStorageContainer();

                foreach (string content in jsonContent)
                {
                    List<ConfigurationSetting> configSetting = JsonConvert.DeserializeObject<List<ConfigurationSetting>>(content)!;
                    foreach (ConfigurationSetting json in configSetting)
                    {
                        configSettings.Add(json);
                    }
                }

                logger.LogInformation(EventIds.BESSJsonFileProcessingCompleted.ToEventId(), "Json file processing Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                return configSettings;
            }
            catch
            {
                logger.LogError(EventIds.BESSJsonFileProcessingFailed.ToEventId(), "Json file Processing failed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
