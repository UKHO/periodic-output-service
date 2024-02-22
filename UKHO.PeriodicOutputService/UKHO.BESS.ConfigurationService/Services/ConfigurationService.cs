using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services;
public class ConfigurationService : IConfigurationService
{
    private readonly IAzureBlobStorageClient azureBlobStorageClient;
    private readonly ILogger<ConfigurationService> logger;
    private readonly IConfigValidator configValidator;

    public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, ILogger<ConfigurationService> logger, IConfigValidator configValidator)
    {
        this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.configValidator = configValidator;
    }

    public void ProcessConfigs()
    {
        try
        {
            Dictionary<string, string> configs = azureBlobStorageClient.GetConfigsInContainer();

            logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}", configs.Keys.Count, CommonHelper.CorrelationID);

            List<BessConfig> bessConfigs = new();
            if (configs.Any())
            {
                int filesWithInvalidAttributeCount = 0;
                foreach (string fileName in configs.Keys.ToList())
                {
                    string content = configs[fileName];

                    bool isValidJson = IsValidJson(content, fileName);

                    if (isValidJson)
                    {
                        List<BessConfig> bessconfig = JsonConvert.DeserializeObject<List<BessConfig>>(content)!;

                        foreach (BessConfig json in bessconfig)
                        {
                            json.FileName = fileName; //for logging
                            ValidationResult results = configValidator.Validate(json);

                            if (!results.IsValid)
                            {
                                filesWithInvalidAttributeCount++;
                                string errors = string.Empty;

                                foreach (var failure in results.Errors)
                                {
                                    errors += "\n" + failure.PropertyName + ": " + failure.ErrorMessage;
                                }

                                logger.LogInformation(EventIds.BessConfigInvalidAttributes.ToEventId(),
                                    "\nBespoke ES is not created for file - " + fileName + ". \nValidation errors - " +
                                    errors + " | _X-Correlation-ID :" + CommonHelper.CorrelationID);
                            }
                            else
                            {
                                bessConfigs.Add(json);
                            }
                        }
                    }
                }

                logger.LogInformation(EventIds.BessConfigInvalidFilesCount.ToEventId(),
                    "\nFile count with invalid attributes   {invalidFileCount} | _X-Correlation-ID : {CorrelationId}",
                    filesWithInvalidAttributeCount, CommonHelper.CorrelationID);

                RemoveDuplicateBessConfigs(bessConfigs);

                //int validFileCount = bessConfigs.Count;
                logger.LogInformation(EventIds.BessConfigValidFilesCount.ToEventId(),
                    "\nFile count with valid attributes   {validFileCount} | _X-Correlation-ID : {CorrelationId}",
                    bessConfigs.Count, CommonHelper.CorrelationID);
            }
            else
            {
                logger.LogWarning(EventIds.BessConfigsNotFound.ToEventId(), "Bess configs not found | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }

            logger.LogInformation(EventIds.BessConfigsProcessingCompleted.ToEventId(),
                "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}",
                CommonHelper.CorrelationID);
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
            if (!token.ToString().Contains("undefined"))
            {
                return true;
            }

            logger.LogWarning(EventIds.BessConfigIsInvalid.ToEventId(), "Bess config is invalid for file : {fileName} | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(EventIds.BessConfigParsingError.ToEventId(), "Error occured while parsing Bess config file:{fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
            return false;
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
                logger.LogInformation("\nBespoke ES is not created for file - " + duplicateBessConfig.FileName +
                                      ". \nValidation errors - duplicate value. Name: " + duplicateBessConfig.Name);

                bessConfigs.RemoveAll(x => x.FileName.Equals(duplicateBessConfig.FileName, StringComparison.OrdinalIgnoreCase));
            }
        }
        logger.LogInformation(EventIds.BessConfigDuplicateFileCount.ToEventId(), "\nFile count with duplicate Name attributes   {duplicateFileCount} | _X-Correlation-ID : {CorrelationId}", duplicateFileCount, CommonHelper.CorrelationID);
    }
}
