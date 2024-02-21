using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<ConfigurationService> logger;

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, ILogger<ConfigurationService> logger)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ProcessConfigs()
        {
            try
            {
                logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "Bess configs processing started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                Dictionary<string, string> configs = azureBlobStorageClient.GetConfigsInContainer();

                List<BessConfig> bessConfigs = new();

                foreach (string fileName in configs.Keys.ToList())
                {
                    string content = configs[fileName];

                    bool isValidJson = IsValidJson(content, fileName);

                    if (isValidJson)
                    {
                        List<BessConfig> bessconfig = JsonConvert.DeserializeObject<List<BessConfig>>(content)!;

                        foreach (BessConfig json in bessconfig)
                        {
                            bessConfigs.Add(json);
                        }
                    }
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
            var token = JToken.Parse(json);

            if (!token.ToString().Contains("undefined"))
            {
                return true;
            }

            logger.LogWarning(EventIds.BessConfigIsInValid.ToEventId(), "Bess config is invalid for file : {fileName} | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
            return false;
        }
    }
}
