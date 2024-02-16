using FluentValidation.Results;
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
        private readonly IConfigValidator configValidator;

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, ILogger<ConfigurationService> logger, IConfigValidator configValidator)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configValidator = configValidator;
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
                        ValidationResult results = await configValidator.Validate(json);

                        if (!results.IsValid)
                        {
                            using (logger.BeginScope("Validation failed for file {file}", json))
                            //using (LogContext.PushProperty("Validation failed for file {file}", file))
                            {
                                string errors = string.Empty;

                                foreach (var failure in results.Errors)
                                {
                                    errors += "\n" + failure.PropertyName + ": " + failure.ErrorMessage;
                                }
                                logger.LogInformation("\nBespoke ES is not created for file - " + json + ". \nValidation errors - " + errors);
                            }
                        }
                        else
                        {
                            configSettings.Add(json);
                        }
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
