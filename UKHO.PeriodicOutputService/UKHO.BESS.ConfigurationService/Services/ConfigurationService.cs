using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

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
                logger.LogInformation(EventIds.BESSJsonFileProcessingStarted.ToEventId(), "Json file processing started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                var configs = azureBlobStorageClient.GetConfigsInContainer();

                //validate
                //Deserialize
                //schema validation
                //attribute validation

                logger.LogInformation(EventIds.BESSJsonFileProcessingCompleted.ToEventId(), "Json file processing Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch
            {
                logger.LogError(EventIds.BESSJsonFileProcessingFailed.ToEventId(), "Json file Processing failed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
