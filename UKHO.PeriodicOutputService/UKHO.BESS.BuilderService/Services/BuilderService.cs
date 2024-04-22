using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Extensions;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.Services
{
    public class BuilderService : IBuilderService
    {
        private readonly IEssService essService;
        private readonly IFssService fssService;
        private readonly IConfiguration configuration;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly ILogger<BuilderService> logger;
        private readonly IOptions<FssApiConfiguration> fssApiConfig;

        private readonly string homeDirectoryPath;

        private const string BessBatchFileExtension = "zip";
        private readonly string mimeType = "application/zip";

        public BuilderService(IEssService essService, IFssService fssService, IConfiguration configuration, IFileSystemHelper fileSystemHelper, ILogger<BuilderService> logger, IOptions<FssApiConfiguration> fssApiConfig)
        {
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
            this.fssService = fssService ?? throw new ArgumentNullException(nameof(fssService));
            this.configuration = configuration;
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fssApiConfig = fssApiConfig ?? throw new ArgumentNullException(nameof(fssApiConfig));
            homeDirectoryPath = Path.Combine(configuration["HOME"], configuration["BespokeFolderName"]);
        }

        public async Task<string> CreateBespokeExchangeSetAsync(ConfigQueueMessage configQueueMessage)
        {
            string essBatchId = await RequestExchangeSetAsync(configQueueMessage);
            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSetAsync(essBatchId);

            ExtractExchangeSetZip(essFiles, essFileDownloadPath);

            var exchangeSetPath = Path.Combine(homeDirectoryPath, essBatchId, fssApiConfig.Value.BespokeExchangeSetFileFolder);

            await PerformAncillaryFilesOperationsAsync(essBatchId, exchangeSetPath, configQueueMessage.CorrelationId, configQueueMessage.ReadMeSearchFilter);

            //Temporary Upload Code
            #region TemporaryUploadCode
            CreateZipFile(essFiles, essFileDownloadPath);

            bool isBatchCreated = false;
            if (bool.Parse(configuration["IsFTRunning"]))
            {   
                if (configQueueMessage.ReadMeSearchFilter.Contains(ReadMeSearchFilter.AVCS.ToString()))
                {
                    isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BessBatchFileExtension, Batch.BesAvcsReadmeBatch).Result;
                }
                else if (configQueueMessage.ReadMeSearchFilter.Contains(ReadMeSearchFilter.BLANK.ToString()))
                {
                    isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BessBatchFileExtension, Batch.BesBlankReadmeBatch).Result;
                }
                else if (configQueueMessage.ReadMeSearchFilter.Contains(ReadMeSearchFilter.NONE.ToString()))
                {
                    isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BessBatchFileExtension, Batch.BesNoneReadmeBatch).Result;
                }
                isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BessBatchFileExtension, Batch.BesValidReadmeBatch).Result;
            }
            else
            {
                isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BessBatchFileExtension, configQueueMessage.Type == BessType.BASE.ToString() ? Batch.BesBaseZipBatch : Batch.BesUpdateZipBatch).Result;
            }

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
                ProductVersionsRequest productVersionsRequest = GetProductVersionDetails(configQueueMessage.EncCellNames);
                exchangeSetResponseModel = await essService.GetProductDataProductVersions(productVersionsRequest, configQueueMessage.ExchangeSetStandard);
            }

            logger.LogInformation(EventIds.ProductsFetchedFromESS.ToEventId(),
                "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}",
                configQueueMessage.EncCellNames.Count(), exchangeSetResponseModel.ExchangeSetCellCount, exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
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

        private ProductVersionsRequest GetProductVersionDetails(IEnumerable<string> encCellNames)
        {
            ProductVersionsRequest request = new()
            {
                ProductVersions = new List<ProductVersion>()
            };

            foreach (string item in encCellNames)
            {
                request.ProductVersions.Add(new ProductVersion { ProductName = item, EditionNumber = 0, UpdateNumber = 0 });
            }
            return request;
        }

        private async Task<List<FssBatchFile>> GetBatchFilesAsync(string essBatchId)
        {
            GetBatchResponseModel batchDetail = await fssService.GetBatchDetails(essBatchId);
            List<FssBatchFile> batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href, FileSize = a.FileSize }).ToList();

            if (batchFiles.Any() && !batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
            {
                return batchFiles;
            }

            logger.LogError(EventIds.ErrorFileFoundInBatch.ToEventId(), "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.UtcNow, CommonHelper.CorrelationID);
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
                IEnumerable<string> filePath = fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);
                UploadBatchFiles(filePath, batchId, batchType);
                isCommitted = await fssService.CommitBatch(batchId, filePath, batchType);
                logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "Batch is created and added to FSS. BatchId: {batchId} and status: {isCommitted}", batchId, isCommitted);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Batch creation failed with Exception : {ex}", ex);
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

        private async Task PerformAncillaryFilesOperationsAsync(string batchId, string exchangeSetPath, string correlationId, string readMeSearchFilter)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fssApiConfig.Value.EncRoot);
            var readMeFilePath = Path.Combine(exchangeSetRootPath, fssApiConfig.Value.ReadMeFileName);

            if (readMeSearchFilter == ReadMeSearchFilter.AVCS.ToString())
                return;
            else if (readMeSearchFilter == ReadMeSearchFilter.BLANK.ToString())
                fileSystemHelper.CreateEmptyFileContent(readMeFilePath);
            else
                await DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId, readMeSearchFilter);
        }

        private async Task<bool> DownloadReadMeFileAsync(string batchId, string exchangeSetRootPath, string correlationId, string readMeSearchFilter)
        {
            bool isDownloadReadMeFileSuccess = false;
            string readMeFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceReadMeFileRequestStart,
                  EventIds.QueryFileShareServiceReadMeFileRequestCompleted,
                  "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () =>
                  {
                      return await fssService.SearchReadMeFilePathAsync(batchId, correlationId, readMeSearchFilter);
                  },
               batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;
                isDownloadReadMeFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
                   EventIds.DownloadReadMeFileRequestCompleted,
                   "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () =>
                   {
                       return await fssService.DownloadReadMeFileAsync(readMeFilePath, batchId, exchangeSetRootPath, correlationId);
                   },
                batchId, correlationId);

                DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;
            }

            return isDownloadReadMeFileSuccess;
        }
    }
}
