using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class BessConfigurationService : IBessConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly ILogger<BessConfigurationService> logger;
        private readonly IConfigValidator configValidator;

        public BessConfigurationService(IAzureBlobStorageClient azureBlobStorageClient, ILogger<BessConfigurationService> logger, IConfigValidator configValidator)
        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configValidator = configValidator;
        }

        public List<BessConfig> ProcessConfigs()
        {
            try
            {
                logger.LogInformation(EventIds.BessJsonFileProcessingStarted.ToEventId(), "Json file processing started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                IDictionary<string, string> configs = azureBlobStorageClient.GetConfigsInContainer();

                List<BessConfig> bessConfigs = new();

                foreach (string fileName in configs.Keys.ToList())
                {
                    string content = configs[fileName];

                    bool isValidJson = IsValidJson(content, fileName);

                    if (isValidJson)
                    {
                        List<BessConfig> deserializedBessConfigs = JsonConvert.DeserializeObject<List<BessConfig>>(content)!;

                        foreach (BessConfig json in deserializedBessConfigs)
                        {
                            json.FileName = fileName; //for logging
                            ValidationResult results = configValidator.Validate(json);

                            if (!results.IsValid)
                            {
                                string errors = string.Empty;

                                foreach (var failure in results.Errors)
                                {
                                    errors += "\n" + failure.PropertyName + ": " + failure.ErrorMessage;
                                }
                                logger.LogInformation("\nBespoke ES is not created for file - " + fileName + ". \nValidation errors - " + errors);
                            }
                            else
                                bessConfigs.Add(json);
                        }
                    }
                }

                RemoveDuplicateBessConfigs(bessConfigs);

                //         //find duplicates with property name
                //         var dupes = bessConfigs.GroupBy(x => new { x.Name })
                //.Where(x => x.Skip(1).Any()).ToList();

                //         //var dupes = bessConfigs.GroupBy(x => new { x.Name }).ToList();
                //         //.Where(x => x.Skip(1).Any());
                //         if (dupes.Any())
                //         {
                //             List<BessConfig> duplicateBessConfigs = new();
                //             int dupCount = dupes.Count();
                //             for (int i = 0; i < dupCount; i++)
                //             {
                //                 foreach (var duplicateConfigs in dupes[i].ToList())
                //                 {
                //                     duplicateBessConfigs.Add(duplicateConfigs);
                //                     logger.LogInformation("\nBespoke ES is not created for file - " + duplicateConfigs.FileName + ". \nValidation errors - Config with duplicate Name attribute value found");
                //                     bessConfigs.RemoveAll(x => x.FileName.Equals(duplicateConfigs.FileName, StringComparison.OrdinalIgnoreCase));
                //                 }
                //             }
                //         }
                logger.LogInformation(EventIds.BessJsonFileProcessingCompleted.ToEventId(), "Json file processing completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                return bessConfigs;
            }
            catch
            {
                logger.LogError(EventIds.BessJsonFileProcessingFailed.ToEventId(), "Json file Processing failed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
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

            logger.LogWarning(EventIds.BessJsonIsNotValid.ToEventId(), "Json is invalid for file : {fileName} | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
            return false;
        }

        private void RemoveDuplicateBessConfigs(List<BessConfig> bessConfigs)
        {
            //find duplicates with property value Name
            var duplicateRecords = bessConfigs.GroupBy(x => new { x.Name })
   .Where(x => x.Skip(1).Any()).ToList();

            if (duplicateRecords.Any())
            {
                //List<BessConfig> duplicateBessConfigs = new();
                int dupCount = duplicateRecords.Count();
                for (int i = 0; i < dupCount; i++)
                {
                    foreach (var duplicateConfigs in duplicateRecords[i].ToList())
                    {
                        //duplicateBessConfigs.Add(duplicateConfigs);
                        logger.LogInformation("\nBespoke ES is not created for file - " + duplicateConfigs.FileName + ". \nValidation errors - Config with duplicate Name attribute value found");
                        bessConfigs.RemoveAll(x => x.FileName.Equals(duplicateConfigs.FileName, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
        }
    }
}
