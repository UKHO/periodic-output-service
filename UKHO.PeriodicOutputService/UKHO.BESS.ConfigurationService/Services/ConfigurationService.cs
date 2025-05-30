﻿using System.Net;
using System.Text;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.ConfigurationService.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<ConfigurationService> logger;
        private readonly ISalesCatalogueService salesCatalogueService;
        private readonly IConfiguration configuration;
        private const string UndefinedValue = "undefined";
        private readonly IConfigValidator configValidator;
        private readonly IAzureBlobStorageService azureBlobStorageService;
        private readonly IMacroTransformer macroTransformer;
        private int filesWithJsonErrorCount;
        private int configsWithDuplicateNameAttributeCount;
        private int configsWithInvalidMacrosCount;
        private const string NewLine = "\n";
        private const string Colon = ": ";
        private const string IsEnabled = "IsEnabled";

        public ConfigurationService(IAzureBlobStorageClient azureBlobStorageClient,
                                    IAzureTableStorageHelper azureTableStorageHelper,
                                    ILogger<ConfigurationService> logger,
                                    IConfigValidator configValidator,
                                    ISalesCatalogueService salesCatalogueService,
                                    IConfiguration configuration,
                                    IAzureBlobStorageService azureBlobStorageService,
                                    IMacroTransformer macroTransformer)

        {
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            this.salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.azureBlobStorageService = azureBlobStorageService ?? throw new ArgumentNullException(nameof(azureBlobStorageService));
            this.macroTransformer = macroTransformer ?? throw new ArgumentNullException(nameof(macroTransformer));
        }

        /// <summary>
        /// Process config from azure container, validate and add config details to queue message
        /// </summary>
        /// <returns></returns>
        public async Task<string> ProcessConfigsAsync()
        {
            try
            {
                var configsInContainer = await azureBlobStorageClient.GetConfigsInContainerAsync();
                if (!configsInContainer.Any())
                {
                    logger.LogWarning(EventIds.BessConfigsNotFound.ToEventId(), "BESS configs not found | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                    return "BESS configs not found";
                }

                logger.LogInformation(EventIds.BessConfigsProcessingStarted.ToEventId(), "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}", configsInContainer.Keys.Count, CommonHelper.CorrelationID);

                IList<BessConfig> bessConfigs = new List<BessConfig>();

                int configsWithInvalidAttributeCount = 0, deserializedConfigsCount = 0;

                foreach (string fileName in configsInContainer.Keys.ToList())
                {
                    string content = configsInContainer[fileName];

                    var deserializedConfig = DeserializeConfig(content, fileName);

                    if (deserializedConfig.isValid)
                    {
                        deserializedConfigsCount++;

                        deserializedConfig.config.FileName = fileName; //for logging
                        try
                        {
                            ValidationResult results = configValidator.Validate(deserializedConfig.config);

                            if (!results.IsValid)
                            {
                                configsWithInvalidAttributeCount++;

                                StringBuilder errors = new();
                                StringBuilder warnings = new();

                                foreach (var failure in results.Errors)
                                {
                                    if (failure.PropertyName == IsEnabled)
                                    {
                                        warnings.AppendLine(NewLine + failure.PropertyName + Colon + failure.ErrorMessage);
                                    }
                                    else
                                    {
                                        errors.AppendLine(NewLine + failure.PropertyName + Colon + failure.ErrorMessage);
                                    }
                                }

                                if (!string.IsNullOrEmpty(errors.ToString()))
                                    logger.LogError(EventIds.BessConfigInvalidAttributes.ToEventId(),
                                        "BESS config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}",
                                        fileName, errors, CommonHelper.CorrelationID);

                                if (!string.IsNullOrEmpty(warnings.ToString()))
                                    logger.LogWarning(EventIds.BessConfigIsNotEnable.ToEventId(),
                                        "BESS config file : {fileName} will be skipped for exchange set creation since the attribute value for IsEnabled is not Yes. Message : {Message} | _X-Correlation-ID : {CorrelationId}",
                                        fileName, warnings, CommonHelper.CorrelationID);
                            }
                            else
                            {
                                bessConfigs.Add(deserializedConfig.config);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(EventIds.BessConfigValidationError.ToEventId(), "Error occurred while validating BESS config file : {fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                            throw new FulfilmentException(EventIds.BessConfigValidationError.ToEventId());
                        }
                    }
                }

                RemoveDuplicateConfigs((List<BessConfig>)bessConfigs);

                if (bessConfigs.Any())
                {
                    configsWithInvalidMacrosCount = TransformMacros(bessConfigs);
                }

                int totalConfigCount = deserializedConfigsCount + filesWithJsonErrorCount;

                logger.LogInformation(EventIds.BessConfigValidationSummary.ToEventId(),
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}", totalConfigCount, bessConfigs.Count, configsWithInvalidAttributeCount, filesWithJsonErrorCount, configsWithDuplicateNameAttributeCount, configsWithInvalidMacrosCount, CommonHelper.CorrelationID);

                if (bessConfigs.Any())
                {
                    var salesCatalogueDataResponse = await salesCatalogueService.GetSalesCatalogueData();

                    await CheckConfigFrequencyAndSaveQueueDetailsAsync(bessConfigs, salesCatalogueDataResponse.ResponseBody);
                }

                logger.LogInformation(EventIds.BessConfigsProcessingCompleted.ToEventId(), "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
                return "BESS configs processing completed";
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigsProcessingFailed.ToEventId(), "BESS configs processing failed with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }

        /// <summary>
        /// Deserialize config
        /// </summary>
        /// <param name="json"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private (BessConfig config, bool isValid) DeserializeConfig(string json, string fileName)
        {
            BessConfig bessConfig = new();
            bool isValid = false;
            try
            {
                var token = JToken.Parse(json);

                if (token.ToString().Contains(UndefinedValue))
                {
                    filesWithJsonErrorCount++;
                    logger.LogWarning(EventIds.BessConfigValueNotDefined.ToEventId(), "BESS config file : {fileName} contains undefined values. | _X-Correlation-ID : {CorrelationId}", fileName, CommonHelper.CorrelationID);
                }
                else
                {
                    bessConfig = JsonConvert.DeserializeObject<BessConfig>(json)!;
                    isValid = true;
                }
                return new ValueTuple<BessConfig, bool>(bessConfig, isValid);
            }
            catch (Exception ex)
            {
                filesWithJsonErrorCount++;
                logger.LogError(EventIds.BessConfigParsingError.ToEventId(), "Error occurred while parsing BESS config file : {fileName}. It might have missing or extra commas, missing brackets, or other syntax errors.| Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", fileName, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                return new ValueTuple<BessConfig, bool>(bessConfig, isValid);
            }
        }

        /// <summary>
        /// Remove duplicate configs
        /// </summary>
        /// <param name="bessConfigs"></param>
        private void RemoveDuplicateConfigs(List<BessConfig> bessConfigs)
        {
            //find duplicates with property Name
            var duplicateRecords = bessConfigs.GroupBy(x => new { x.Name })
                .Where(x => x.Skip(1).Any()).ToList();

            if (duplicateRecords.Any())
            {
                configsWithDuplicateNameAttributeCount = duplicateRecords.Select(record => record.Count()).Sum();

                foreach (var duplicateConfig in duplicateRecords.SelectMany(duplicateRecord => duplicateRecord.ToList()))
                {
                    logger.LogWarning(EventIds.BessConfigsDuplicateRecordsFound.ToEventId(),
                        "BESS config file : {fileName} found with duplicate Name attribute : {name} | _X-Correlation-ID : {CorrelationId}",
                        duplicateConfig.FileName, duplicateConfig.Name, CommonHelper.CorrelationID);

                    bessConfigs.RemoveAll(x =>
                        x.FileName.Equals(duplicateConfig.FileName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        /// <summary>
        /// Check config frequency and save details to message queue
        /// </summary>
        /// <param name="bessConfigs"></param>
        /// <param name="salesCatalogueDataProducts"></param>
        /// <returns></returns>
        public async Task<bool> CheckConfigFrequencyAndSaveQueueDetailsAsync(IList<BessConfig> bessConfigs, IList<SalesCatalogueDataProductResponse> salesCatalogueDataProducts)
        {
            try
            {
                var productVersionEntities = await azureTableStorageHelper.GetLatestBessProductVersionDetailsAsync();

                foreach (var config in bessConfigs)
                {
                    // Parse the cron expression using NCronTab library
                    var schedule = CrontabSchedule.Parse(config.Frequency);

                    // Get the next occurrence of the cron expression after the last execution time
                    DateTime nextOccurrence = schedule.GetNextOccurrence(DateTime.UtcNow);
                    ScheduleDetailEntity existingScheduleDetail = await GetScheduleDetailAsync(nextOccurrence, config);

                    if (CheckSchedule(config, existingScheduleDetail)) //Check if config schedule is missed or if it's due for the same day.
                    {
                        logger.LogInformation(EventIds.BessConfigFrequencyElapsed.ToEventId(), "BESS Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}," +
                            " Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}", config.Name, config.Frequency, existingScheduleDetail.NextScheduleTime, DateTime.UtcNow, CommonHelper.CorrelationID);

                        await azureTableStorageHelper.UpsertScheduleDetailAsync(nextOccurrence, config, true);

                        var encCells = GetEncCells(config.EncCellNames, salesCatalogueDataProducts).ToList();

                        if (!encCells.Any()) //If cells are not found then bespoke exchange set will not create
                        {
                            logger.LogWarning(EventIds.BessEncCellNamesAndPatternNotFoundInSalesCatalogue.ToEventId(), "Neither listed ENC cell names found nor the pattern matched for any cell, Bespoke Exchange Set will not be created for : {EncCellNames} |" +
                                " _X-Correlation-ID : {CorrelationId}", string.Join(", ", config.EncCellNames), CommonHelper.CorrelationID);

                            continue;
                        }

                        long totalFileSize = 0;

                        totalFileSize = GetTotalFileSizeForBase(encCells);
                        
                        var fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(totalFileSize!);

                        var BESSize = Convert.ToInt16(configuration["BessSizeInMB"]);

                        logger.LogInformation(EventIds.BaseExchangeSetSizeCalculated.ToEventId(), "Base exchange set size for file:{FileName} is: {fileSizeInMb} | _X-Correlation-ID : {CorrelationId}", config.FileName, fileSizeInMb, CommonHelper.CorrelationID);

                        //Bespoke will not create if size is large
                        if (fileSizeInMb > BESSize)
                        {
                            if (config.Type == BessType.UPDATE.ToString() ||
                                config.Type == BessType.CHANGE.ToString())
                            {
                                totalFileSize = await GetTotalFileSizeForUpdateAsync(encCells, productVersionEntities, config);
                                fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(totalFileSize);

                                logger.LogInformation(EventIds.UpdateExchangeSetSizeCalculated.ToEventId(), "{Type} exchange set size for file:{FileName} is: {fileSizeInMb} | _X-Correlation-ID : {CorrelationId}", config.Type.ToUpper(), config.FileName, fileSizeInMb, CommonHelper.CorrelationID);
                            }

                            if (fileSizeInMb > BESSize)
                            {
                                logger.LogWarning(EventIds.BessSizeExceedsThreshold.ToEventId(),
                                    "Bespoke Exchange Set size {fileSizeInMb}MB which is more than the threshold :{BESSize}MB, Bespoke Exchange Set for type : {Type} will not be created for file : {FileName} | _X-Correlation-ID : {CorrelationId}",
                                    Math.Round(fileSizeInMb, 2), BESSize, config.Type, config.FileName, CommonHelper.CorrelationID);
                                continue;
                            }
                        }

                        //--save details to message queue --
                        var encCellNames = encCells.Select(i => i.Item1).ToList();

                        var success = await azureBlobStorageService.SetConfigQueueMessageModelAndAddToQueueAsync(config, encCellNames, totalFileSize);

                        if (success)
                        {
                            logger.LogInformation(EventIds.BessQueueMessageSuccessful.ToEventId(), "Queue message creation successful for file:{FileName} | _X-Correlation-ID : {CorrelationId}", config.FileName, CommonHelper.CorrelationID);
                        }
                        else
                        {
                            logger.LogWarning(EventIds.BessQueueMessageFailed.ToEventId(), "Something went wrong while adding message to queue, Bespoke Exchange Set will not be created for file : {FileName} | _X-Correlation-ID : {CorrelationId}", config.FileName, CommonHelper.CorrelationID);
                        }
                    }
                    else
                    {   //Update schedule details
                        if (IsScheduleRefreshed(existingScheduleDetail, nextOccurrence, config))
                        {
                            await azureTableStorageHelper.UpsertScheduleDetailAsync(nextOccurrence, config, false);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessConfigFrequencyProcessingException.ToEventId(), "Exception occurred while processing BESS config {DateTime} | {ErrorMessage} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.BessConfigFrequencyProcessingException.ToEventId());
            }
        }
        
        private  async Task<long> GetTotalFileSizeForUpdateAsync(IEnumerable<(string, int?)> encCells,
            List<ProductVersionEntities> productVersionEntities, BessConfig config)
        {
            long totalFileSize = 0;

            var productVersions = CommonHelper.GetProductVersionsFromEntities(productVersionEntities, encCells.Select(i => i.Item1), config.Name, config.ExchangeSetStandard);

            var salesCatalogueResponse = await salesCatalogueService.PostProductVersionsAsync(productVersions);

            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                totalFileSize = salesCatalogueResponse.ResponseBody.Products?.Sum(p => p.FileSize.GetValueOrDefault()) ?? 0;
            }

            return totalFileSize;
        }


        private static long GetTotalFileSizeForBase(IEnumerable<(string, int?)> encCells)
        {
            return encCells.Sum(encCell => (long)encCell.Item2!);
        }
        
        /// <summary>
        /// If schedule details is different from table then update details
        /// </summary>
        /// <param name="scheduleDetailEntity"></param>
        /// <param name="nextOccurrence"></param>
        /// <param name="bessConfig"></param>
        /// <returns></returns>
        private static bool IsScheduleRefreshed(ScheduleDetailEntity scheduleDetailEntity, DateTime nextOccurrence, BessConfig bessConfig) => scheduleDetailEntity.NextScheduleTime != nextOccurrence || scheduleDetailEntity.IsEnabled != bessConfig.IsEnabled;

        /// <summary>
        /// Check schedule interval arrives for same day
        /// </summary>
        /// <param name="bessConfig"></param>
        /// <param name="scheduleDetailEntity"></param>
        /// <returns></returns>
        private static bool CheckSchedule(BessConfig bessConfig, ScheduleDetailEntity scheduleDetailEntity)
        {
            int intervalInMinutes = ((int)scheduleDetailEntity.NextScheduleTime.Subtract(DateTime.UtcNow).TotalSeconds);
            bool isSameDay = scheduleDetailEntity.NextScheduleTime.Date.Subtract(DateTime.UtcNow.Date).Days == 0;

            return intervalInMinutes <= 0 && isSameDay && bessConfig.IsEnabled.ToLower().Equals("yes");
        }

        /// <summary>
        /// Get Schedule details
        /// </summary>
        /// <param name="nextOccurrence"></param>
        /// <param name="bessConfig"></param>
        /// <returns></returns>
        private async Task<ScheduleDetailEntity> GetScheduleDetailAsync(DateTime nextOccurrence, BessConfig bessConfig)
        {
            var existingScheduleDetail = await azureTableStorageHelper.GetScheduleDetailAsync(bessConfig.Name);

            if (existingScheduleDetail != null)
            {
                return existingScheduleDetail;
            }

            await azureTableStorageHelper.UpsertScheduleDetailAsync(nextOccurrence, bessConfig, false);

            ScheduleDetailEntity scheduleDetailEntity = new();
            scheduleDetailEntity.NextScheduleTime = nextOccurrence;
            scheduleDetailEntity.IsEnabled = bessConfig.IsEnabled;

            return scheduleDetailEntity;
        }

        /// <summary>
        /// Fetched the list of all the ENC cell names from the provided prefix patterns
        /// AIO cell is excluded
        /// </summary>
        /// <param name="encCellNames"></param>
        /// <param name="salesCatalogueProducts"></param>
        /// <returns></returns>
        private IEnumerable<(string, int?)> GetEncCells(IEnumerable<string> encCellNames, IEnumerable<SalesCatalogueDataProductResponse> salesCatalogueProducts)
        {
            //filter provided prefix patterns
            const string Pattern = "*";
            List<string> ignoreList = new();
            var encCells = encCellNames.Where(i => i.EndsWith(Pattern));

            foreach (string encCell in encCells)
            {
                ignoreList.AddRange(encCellNames.Where(x => x.StartsWith(encCell.Remove(encCell.Length - 1)) && !x.Equals(encCell)));
            }

            var prefixPatterns = encCellNames.Where(y => !ignoreList.Any(z => z.Equals(y)));

            //get enc cells from provided prefix patterns
            List<(string, int?)> filteredEncCell = new();
            List<string> invalidPatternOrCell = new();

            foreach (var prefixPattern in prefixPatterns)
            {
                var salesCatalogueDataProducts = prefixPattern.EndsWith(Pattern) ? salesCatalogueProducts.Where(x => x.ProductName.StartsWith(prefixPattern.Remove(prefixPattern.Length - 1)))
                                                                                                                        : salesCatalogueProducts.Where(x => x.ProductName.Equals(prefixPattern));

                if (salesCatalogueDataProducts.Any())
                {
                    filteredEncCell.AddRange(salesCatalogueDataProducts.Select(t => (t.ProductName, t.FileSize)));
                }
                else //add invalid cells
                {
                    invalidPatternOrCell.Add(prefixPattern);
                }
            }
            //Apart from valid, if invalid pattern or cell found then log
            if (invalidPatternOrCell.Any() && filteredEncCell.Any())
            {
                logger.LogWarning(EventIds.BessInvalidEncCellNamesOrPatternNotFoundInSalesCatalogue.ToEventId(), "Invalid pattern or ENC cell names found : {InvalidEncCellName} | AIO cells to be excluded : {AIOCellName} | _X-Correlation-ID : {CorrelationId}", string.Join(", ", invalidPatternOrCell), string.Join(", ", configuration["AioCells"]), CommonHelper.CorrelationID);
            }

            //remove aio cells and return all filtered data
            return filteredEncCell.Where(x => !configuration["AioCells"].Split(",").Any(i => i.Equals(x.Item1)));
        }

        /// <summary>
        /// Transform Macros
        /// </summary>
        /// <param name="bessConfigs"></param>
        /// <returns></returns>
        private int TransformMacros(IList<BessConfig> bessConfigs)
        {
            List<BessConfig> configsWithInvalidMacros = new();

            foreach (BessConfig config in bessConfigs)
            {
                try
                {
                    foreach (Tag tag in config.Tags.Where(tag => tag.Value.StartsWith('$')))
                    {
                        string transformedValue = macroTransformer.ExpandMacros(tag.Value);
                        if (tag.Value.Equals(transformedValue))
                        {
                            configsWithInvalidMacros.Add(config);

                            logger.LogError(EventIds.MacroInvalidOrUnavailable.ToEventId(), "Macro {macro} is invalid or not available, Bespoke Exchange Set will not be created for config file: {fileName} | _X-Correlation-ID : {CorrelationId}", tag.Value, config.FileName, CommonHelper.CorrelationID);

                            break;
                        }
                        else
                        {
                            tag.Value = transformedValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.MacroTransformationFailed.ToEventId(), "Exception occurred while transforming macros {DateTime} | {ErrorMessage} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.MacroTransformationFailed.ToEventId());
                }
            }

            foreach (BessConfig config in configsWithInvalidMacros)
            {
                bessConfigs.Remove(config);
            }
            return configsWithInvalidMacros.Count;
        }
    }
}
