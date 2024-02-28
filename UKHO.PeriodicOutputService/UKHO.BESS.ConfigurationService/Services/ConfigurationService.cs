using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<ConfigurationService> logger;
        private const string UndefinedValue = "undefined";
        private readonly IConfigValidator configValidator;
        List<string> invalidNameList = new();

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient,
            ILogger<ConfigurationService> logger, IConfigValidator configValidator)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator)); ;
        }

        public void ProcessConfigs()
        {
            try
            {
                Dictionary<string, string> configs = azureBlobStorageClient.GetConfigsInContainer();

                logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}", configs.Keys.Count, CommonHelper.CorrelationID);

                IList<BessConfig> bessConfigs = new List<BessConfig>();

                if (configs.Any())
                {
                    int filesWithInvalidAttributeCount = 0;

                    foreach (string fileName in configs.Keys.ToList())
                    {
                        string content = configs[fileName];

                        IList<BessConfig> bessConfig = GetValidConfig(content, fileName);

                        foreach (BessConfig config in bessConfig)
                        {
                            config.FileName = fileName; //for logging
                            ValidationResult results = configValidator.Validate(config);

                            if (!results.IsValid)
                            {
                                filesWithInvalidAttributeCount++;
                                string errors = string.Empty;

                                foreach (var failure in results.Errors)
                                {
                                    errors += "\n" + failure.PropertyName + ": " + failure.ErrorMessage;
                                }

                                invalidNameList.Add(config.Name);

                                logger.LogInformation(EventIds.BessConfigInvalidAttributes.ToEventId(), "Bespoke ES is not created for file - {fileName}. \nValidation errors - {errors} | _X-Correlation-ID : {CorrelationId}",
                                    fileName, errors, CommonHelper.CorrelationID);
                            }
                            else
                            {
                                bessConfigs.Add(config);
                            }
                        }
                    }

                    logger.LogInformation(EventIds.BessConfigInvalidFilesCount.ToEventId(),
                    "Invalid file count {invalidFileCount} and invalid config name : {invalidFileNames} | _X-Correlation-ID : {CorrelationId}",
                    filesWithInvalidAttributeCount, string.Join(",", invalidNameList), CommonHelper.CorrelationID);

                    RemoveDuplicateBessConfigs((List<BessConfig>)bessConfigs);

                    logger.LogInformation(EventIds.BessConfigValidFilesCount.ToEventId(),
                        "Valid config count : {validFileCount} and valid config names : {validConfigNames} | _X-Correlation-ID : {CorrelationId}",
                        bessConfigs.Count, string.Join(",", bessConfigs.Select(x => x.Name)), CommonHelper.CorrelationID);
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

        private IList<BessConfig> GetValidConfig(string json, string fileName)
        {
            IList<BessConfig> bessConfig = new List<BessConfig>();
            try
            {
                var token = JToken.Parse(json);

                if (token.ToString().Contains(UndefinedValue))
                {
                    logger.LogWarning(EventIds.BessConfigIsInvalid.ToEventId(), "Bess config is invalid for file : {fileName} | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
                }
                else
                {
                    bessConfig = JsonConvert.DeserializeObject<List<BessConfig>>(json)!;
                }
                return bessConfig;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigParsingError.ToEventId(), "Error occurred while parsing Bess config file:{fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                return bessConfig;
            }
        }

        private void RemoveDuplicateBessConfigs(List<BessConfig> bessConfigs)
        {
            //find duplicates with property Name
            var duplicateRecords = bessConfigs.GroupBy(x => new { x.Name })
                .Where(x => x.Skip(1).Any()).ToList();

            if (!duplicateRecords.Any()) return;

            int duplicateFileCount = duplicateRecords.Select(record => record.Count()).Sum();

            foreach (var duplicateRecord in duplicateRecords)
            {
                foreach (BessConfig? duplicateBessConfig in duplicateRecord.ToList())
                {
                    logger.LogError(EventIds.BessConfigsProcessingFailed.ToEventId(), "\nBespoke ES is not created for file : {fileName}.\nValidation errors - duplicate value. Name : {name} | _X-Correlation-ID : {CorrelationId}", duplicateBessConfig.FileName, duplicateBessConfig.Name, CommonHelper.CorrelationID);

                    bessConfigs.RemoveAll(x =>
                        x.FileName.Equals(duplicateBessConfig.FileName, StringComparison.OrdinalIgnoreCase));
                }
            }

            logger.LogInformation(EventIds.BessConfigDuplicateFileCount.ToEventId(),
                "File count with duplicate Name attributes {duplicateFileCount} | _X-Correlation-ID : {CorrelationId}",
                duplicateFileCount, CommonHelper.CorrelationID);
        }
    }
}
