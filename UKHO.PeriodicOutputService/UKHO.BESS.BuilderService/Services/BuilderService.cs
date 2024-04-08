using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.Services
{
    public class BuilderService : IBuilderService
    {
        private readonly IEssService essService;
        private readonly IFssService fssService;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly ILogger<BuilderService> logger;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;

        private const string BESPOKE_FILE_NAME = "V01X01";
        private readonly string homeDirectoryPath;

        private const string BessBatchFileExtension = "zip";
        private readonly string mimeType = "application/zip";

        public BuilderService(IEssService essService, IFssService fssService, IConfiguration configuration, IFileSystemHelper fileSystemHelper, ILogger<BuilderService> logger, IAzureTableStorageHelper azureTableStorageHelper)
        {
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
            this.fssService = fssService ?? throw new ArgumentNullException(nameof(fssService));
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));

            homeDirectoryPath = Path.Combine(configuration["HOME"]!, configuration["BespokeFolderName"]!);
        }

        public async Task<string> CreateBespokeExchangeSetAsync(ConfigQueueMessage configQueueMessage)
        {
            string essBatchId = await RequestExchangeSetAsync(configQueueMessage);
            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSetAsync(essBatchId);

            ExtractExchangeSetZip(essFiles, essFileDownloadPath);
            
            //Temporary Upload Code
            #region TemporaryUploadCode
            CreateZipFile(essFiles, essFileDownloadPath);

            bool isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BessBatchFileExtension, Batch.BesBaseZipBatch).Result;

            // temporary logs
            if (isBatchCreated)
            {
                logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "Bess Base batch created {DateTime} | {CorrelationId}", DateTime.UtcNow, configQueueMessage.CorrelationId);
            }
            else
            {
                logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Bess Base batch failed {DateTime} | {CorrelationId}", DateTime.UtcNow, configQueueMessage.CorrelationId);
            }

            #endregion TemporaryUploadCode

            var latestProductVersions = GetTheLatestUpdateNumber(essFileDownloadPath, configQueueMessage.EncCellNames.ToArray());

            LogProductVersions(latestProductVersions, configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);

            return "Exchange Set Created Successfully";
        }

        private async Task<string> RequestExchangeSetAsync(ConfigQueueMessage configQueueMessage)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = new();
            if (configQueueMessage.Type == BessType.BASE.ToString())
            {
                exchangeSetResponseModel = await essService.PostProductIdentifiersData((List<string>)configQueueMessage.EncCellNames, configQueueMessage.ExchangeSetStandard);
            }
            else if (configQueueMessage.Type == BessType.UPDATE.ToString() ||
                     configQueueMessage.Type == BessType.CHANGE.ToString())
            {

                var productVersionEntities = await azureTableStorageHelper.GetLatestBessProductVersionDetailsAsync();

                var productVersions = GetProductVersionsFromEntities(productVersionEntities, configQueueMessage.EncCellNames.ToArray(), configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);

                exchangeSetResponseModel = await essService.GetProductDataProductVersions(new ProductVersionsRequest()
                {
                    ProductVersions = productVersions
                }, configQueueMessage.ExchangeSetStandard);
            }

            return CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href!);
        }

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSetAsync(string essBatchId)
        {
            string downloadPath = Path.Combine(homeDirectoryPath, essBatchId);
            List<FssBatchFile> files;

            FssBatchStatus fssBatchStatus = await fssService.CheckIfBatchCommitted(essBatchId, RequestType.BESS);

            if (fssBatchStatus == FssBatchStatus.Committed)
            {
                fileSystemHelper.CreateDirectory(downloadPath);
                files = await GetBatchFilesAsync(essBatchId);
                DownloadFiles(files, downloadPath);
            }
            else
            {
                logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), fssBatchStatus, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
            }

            return (downloadPath, files);
        }

        private async Task<List<FssBatchFile>> GetBatchFilesAsync(string essBatchId)
        {
            GetBatchResponseModel batchDetail = await fssService.GetBatchDetails(essBatchId);
            List<FssBatchFile> batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href, FileSize = a.FileSize }).ToList();

            if (batchFiles.Any() && !batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
            {
                return batchFiles;
            }

            logger.LogError(EventIds.ErrorFileFoundInBatch.ToEventId(), "Either no files found or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.UtcNow, CommonHelper.CorrelationID);
            throw new FulfilmentException(EventIds.ErrorFileFoundInBatch.ToEventId());
        }

        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                fssService.DownloadFileAsync(file.FileName, file.FileLink, file.FileSize, filePath).Wait();
            });
        }

        private void ExtractExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    logger.LogInformation(EventIds.ExtractZipFileStarted.ToEventId(), "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);

                    logger.LogInformation(EventIds.ExtractZipFileCompleted.ToEventId(), "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ExtractZipFileFailed.ToEventId(), "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Extracting zip file {file.FileName} failed at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        //Temporary Upload Code
        #region Create Bess Batch temporary code
        [ExcludeFromCodeCoverage]
        private void CreateZipFile(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    logger.LogInformation(EventIds.ZipFileCreationStarted.ToEventId(), "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    fileSystemHelper.CreateZipFile(Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), Path.Combine(downloadPath, file.FileName), true);

                    logger.LogInformation(EventIds.ZipFileCreationCompleted.ToEventId(), "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ZipFileCreationFailed.ToEventId(), "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Creating zip file {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        private async Task<bool> CreateBessBatchAsync(string downloadPath, string fileExtension, Batch batchType)
        {
            bool isCommitted;
            try
            {
                string batchId = await fssService.CreateBatch(batchType);
                IEnumerable<string> filePaths = fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);
                UploadBatchFiles(filePaths, batchId, batchType);
                isCommitted = await fssService.CommitBatch(batchId, filePaths, batchType);
                logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "Batch added to FSS. BatchId: {batchId} and status: {isCommitted}", batchId, isCommitted);
            }
            catch (Exception)
            {
                logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Batch create failed with Exception : {ex}", ex);
                throw;
            }

            return isCommitted;
        }

        [ExcludeFromCodeCoverage]
        private void UploadBatchFiles(IEnumerable<string> filePaths, string batchId, Batch batchType)
        {
            Parallel.ForEach(filePaths, filePath =>
            {
                IFileInfo fileInfo = fileSystemHelper.GetFileInfo(filePath);

                bool isFileAdded = fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length, mimeType, batchType).Result;
                if (isFileAdded)
                {
                    List<string> blockIds = fssService.UploadBlocks(batchId, fileInfo).Result;
                    if (blockIds.Count > 0)
                    {
                        bool fileWritten = fssService.WriteBlockFile(batchId, fileInfo.Name, blockIds).Result;
                    }
                }
            });
        }
        #endregion

        private List<ProductVersion> GetProductVersionsFromEntities(List<BessProductVersionEntities> productVersionEntities, string[] cellNames, string configName, string exchangeSetStandard)
        {
            List<ProductVersion> productVersions = new();

            foreach (var cellName in cellNames)
            {
                ProductVersion productVersion = new();

                var result = productVersionEntities.Where(p => p.PartitionKey == configName && p.ProductName == cellName && p.RowKey == exchangeSetStandard + "|" + p.ProductName);

                if (result.Any())
                {
                    productVersion.ProductName = result.FirstOrDefault().ProductName;
                    productVersion.EditionNumber = result.FirstOrDefault().EditionNumber;
                    productVersion.UpdateNumber = result.FirstOrDefault().UpdateNumber;
                }
                else
                {
                    productVersion.ProductName = cellName;
                    productVersion.EditionNumber = 0;
                    productVersion.UpdateNumber = 0;
                }
                productVersions.Add(productVersion);
            }

            return productVersions;
        }

        private ProductVersionsRequest GetTheLatestUpdateNumber(string filePath, string[] cellNames)
        {
            string exchangeSetPath = Path.Combine(filePath, BESPOKE_FILE_NAME);

            ProductVersionsRequest productVersionsRequest = new()
            {
                ProductVersions = new()
            };

            foreach (var cellName in cellNames)
            {
                var productVersions = fileSystemHelper.GetProductVersionsFromDirectory(exchangeSetPath, cellName);

                productVersionsRequest.ProductVersions.AddRange(productVersions);
            }
            return productVersionsRequest;
        }

        private void LogProductVersions(ProductVersionsRequest productVersionsRequest, string name, string exchangeSetStandard)
        {
            try
            {
                logger.LogInformation(EventIds.LoggingProductVersionsStarted.ToEventId(), "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                azureTableStorageHelper.SaveBessProductVersionDetailsAsync(productVersionsRequest.ProductVersions, name, exchangeSetStandard);

                logger.LogInformation(EventIds.LoggingProductVersionsCompleted.ToEventId(), "Logging product version completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.LoggingProductVersionsFailed.ToEventId(), "Logging product version failed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                throw new Exception($"Logging Product version failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
            }
        }
    }
}
