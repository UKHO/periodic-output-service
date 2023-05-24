using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.AdmiraltyInformationOverlay.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IEssService _essService;
        private readonly IFssService _fssService;
        private readonly ILogger<FulfilmentDataService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAzureTableStorageHelper _azureTableStorageHelper;

        private readonly string _homeDirectoryPath;

        private const string ESSVALIDATIONREASONFORCANCELLEDPRODUCT = "noDataAvailableForCancelledProduct";
        private const string AIOBASEZIPISOSHA1EXCHANGESETFILEEXTENSION = "zip;iso;sha1";
        private const string UPDATEZIPEXCHANGESETFILEEXTENSION = "zip";
        private const string DEFAULTMIMETYPE = "application/octet-stream";

        private readonly Dictionary<string, string> _mimeTypes = new()
        {
            { ".zip", "application/zip" },
            { ".iso", "application/x-raw-disk-image" },
            { ".sha1", "text/plain" }
        };

        public FulfilmentDataService(IFileSystemHelper fileSystemHelper,
                                     IEssService essService,
                                     IFssService fssService,
                                     ILogger<FulfilmentDataService> logger,
                                     IConfiguration configuration,
                                     IAzureTableStorageHelper azureTableStorageHelper)
        {
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _essService = essService ?? throw new ArgumentNullException(nameof(essService));
            _fssService = fssService ?? throw new ArgumentNullException(nameof(fssService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));

            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["AIOFolderName"]);
        }

        public async Task<bool> CreateAioExchangeSetsAsync()
        {
            _fileSystemHelper.CreateDirectory(_homeDirectoryPath);

            bool isSuccess = false;

            _logger.LogInformation(EventIds.GetLatestProductVersionDetailsStarted.ToEventId(), "Getting latest product version details started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var productVersionEntities = _azureTableStorageHelper.GetLatestProductVersionDetails();

            _logger.LogInformation(EventIds.GetLatestProductVersionDetailsCompleted.ToEventId(), "Getting latest product version details completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            Task[] tasks = null;

            Task aioBaseExchangeSetTask = Task.Run(() => CreateAioBaseExchangeSet());
            Task updateAioExchangeSetTask = Task.Run(() => CreateUpdateAIOExchangeSet(productVersionEntities));

            tasks = new Task[] { aioBaseExchangeSetTask, updateAioExchangeSetTask };

            await Task.WhenAll(tasks);

            isSuccess = true;

            return isSuccess;
        }

        private async Task CreateAioBaseExchangeSet()
        {
            _logger.LogInformation(EventIds.AioBaseExchangeSetCreationStarted.ToEventId(), "Creation of AIO base exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            if (!string.IsNullOrEmpty(_configuration["AioCells"]))
            {
                var aioCells = Convert.ToString(_configuration["AioCells"]).Split(',').ToList();

                string essBatchId = await PostProductIdentifiersToESS(aioCells);

                (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSet(essBatchId, Batch.EssAioBaseZipBatch);

                if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
                {
                    ExtractExchangeSetZip(essFiles, essFileDownloadPath);

                    await DownloadAioAncillaryFilesAsync(essBatchId);

                    CreateExchangeSetZip(essFiles, essFileDownloadPath);

                    CreateIsoAndSha1ForExchangeSet(essFiles, essFileDownloadPath);

                    bool isFullAvcsDvdBatchCreated = await CreatePosBatch(essFileDownloadPath, AIOBASEZIPISOSHA1EXCHANGESETFILEEXTENSION, Batch.AioBaseCDZipIsoSha1Batch);
                }
                else
                {
                    _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(), "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
                }
            }
            else
            {
                _logger.LogError(EventIds.AioCellsConfigurationMissing.ToEventId(), "AIO cells are empty in configuration | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.AioCellsConfigurationMissing.ToEventId());
            }
            _logger.LogInformation(EventIds.AioBaseExchangeSetCreationCompleted.ToEventId(), "Creation of AIO base exchange set completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
        }

        private async Task CreateUpdateAIOExchangeSet(List<ProductVersionEntities> productVersionEntities)
        {
            _logger.LogInformation(EventIds.AioUpdateExchangeSetCreationStarted.ToEventId(), "Creation of update exchange set for Productversions - {Productversions} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", productVersionEntities, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string[] aioCellNames = Convert.ToString(_configuration["AioCells"]).Split(',').ToArray();

            var productVersions = GetProductVersionsFromEntities(productVersionEntities, aioCellNames);

            string essBatchId = await GetProductDataVersionFromEss(new ProductVersionsRequest()
            {
                ProductVersions = productVersions
            });

            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSet(essBatchId, Batch.EssUpdateZipBatch);

            if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
            {
                ExtractExchangeSetZip(essFiles, essFileDownloadPath);

                var latestProductVersions = GetTheLatestUpdateNumber(essFileDownloadPath, aioCellNames);

                bool isUpdateZipBatchCreated = await CreatePosBatch(essFileDownloadPath, UPDATEZIPEXCHANGESETFILEEXTENSION, Batch.AioUpdateZipBatch);

                if (isUpdateZipBatchCreated)
                {
                    _logger.LogInformation(EventIds.AioUpdateExchangeSetCreationCompleted.ToEventId(), "Update exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    if (!bool.Parse(_configuration["IsFTRunning"]))
                    {
                        LogProductVersions(latestProductVersions);
                    }
                }
            }
            else
            {
                _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(), "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
            }
        }

        private async Task<string> GetProductDataVersionFromEss(ProductVersionsRequest productVersionsRequest)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = await _essService.GetProductDataProductVersions(productVersionsRequest);

            if (exchangeSetResponseModel.AioExchangeSetCellCount == 0)
            {
                _logger.LogError(EventIds.EssValidationFailed.ToEventId(), "Due to the empty exchange set, ESS validation failed while producing an update | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.EssValidationFailed.ToEventId());
            }

            if (exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Any())
            {
                if (exchangeSetResponseModel.RequestedProductsNotInExchangeSet.All(p => p.Reason == ESSVALIDATIONREASONFORCANCELLEDPRODUCT))
                {
                    _logger.LogInformation(EventIds.CancelledProductsFound.ToEventId(), "{Count} cancelled products found while creating update exchange set and they are [{Products}] on  {DateTime} | _X-Correlation-ID : {CorrelationId}", exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), string.Join(',', exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Select(a => a.ProductName).ToList()), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                else
                {
                    _logger.LogError(EventIds.EssValidationFailed.ToEventId(), "ESS validation failed for {Count} products [{Products}] while creating update exchange set {DateTime} | _X-Correlation-ID : {CorrelationId}", exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), string.Join(',', exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Select(a => a.ProductName + " - " + a.Reason).ToList()), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.EssValidationFailed.ToEventId());
                }
            }

            string essBatchId = CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
            _logger.LogInformation(EventIds.BatchCreatedInESS.ToEventId(), "Batch for Update exchange set created by ESS successfully with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return essBatchId;
        }

        private async Task<string> PostProductIdentifiersToESS(List<string> productIdentifiers)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = await _essService.PostProductIdentifiersData(productIdentifiers);

            string essBatchId = CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
            _logger.LogInformation(EventIds.BatchCreatedInESS.ToEventId(), "Batch for AIO base CD created by ESS successfully with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return essBatchId;
        }

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSet(string essBatchId, Batch batchType)
        {
            string downloadPath = Path.Combine(_homeDirectoryPath, essBatchId);
            List<FssBatchFile> files = new();

            if (!string.IsNullOrEmpty(essBatchId))
            {
                FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId, RequestType.AIO);

                if (fssBatchStatus == FssBatchStatus.Committed)
                {
                    _fileSystemHelper.CreateDirectory(downloadPath);
                    files = await GetBatchFiles(essBatchId);
                    DownloadFiles(files, downloadPath);

                    files = RenameFiles(downloadPath, files, batchType);
                }
                else
                {
                    _logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), fssBatchStatus, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
                }
            }
            return (downloadPath, files);
        }

        private async Task<List<FssBatchFile>> GetBatchFiles(string essBatchId)
        {
            List<FssBatchFile> batchFiles = null;

            GetBatchResponseModel batchDetail = await _fssService.GetBatchDetails(essBatchId);
            batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href, FileSize = a.FileSize }).ToList();

            if (!batchFiles.Any() || batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
            {
                _logger.LogError(EventIds.ErrorFileFoundInBatch.ToEventId(), "Either no files found or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.ErrorFileFoundInBatch.ToEventId());
            }

            if (batchFiles.Any(f => f.FileName.ToLower().Contains("v01x01")))
            {
                _logger.LogError(EventIds.V01X01FileFoundInAIOBatch.ToEventId(), "The configuration of the AIO cell is not synchronized with the ESS. V01X01 file found in AIO batch - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.V01X01FileFoundInAIOBatch.ToEventId());
            }

            return batchFiles;
        }

        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                _fssService.DownloadFileAsync(file.FileName, file.FileLink, file.FileSize, filePath).Wait();
            });
        }

        private List<FssBatchFile> RenameFiles(string downloadPath, List<FssBatchFile> files, Batch batchType)
        {
            foreach (FssBatchFile? file in files)
            {
                IFileInfo fileInfo = _fileSystemHelper.GetFileInfo(Path.Combine(downloadPath, file.FileName));
                string weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();
                string currentYear = DateTime.UtcNow.ToString("yy");
                file.VolumeIdentifier = "V01X01";
                if (batchType == Batch.EssAioBaseZipBatch)
                {
                    file.FileName = string.Format(_configuration["AioBaseZipFileName"], weekNumber, currentYear);
                }
                else
                {
                    file.FileName = string.Format(_configuration["AioUpdateZipFileName"], weekNumber, currentYear);
                }
                fileInfo.MoveTo(Path.Combine(downloadPath, file.FileName));
            }
            return files;
        }

        private void ExtractExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    _logger.LogInformation(EventIds.ExtractZipFileStarted.ToEventId(), "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    _fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);

                    _logger.LogInformation(EventIds.ExtractZipFileCompleted.ToEventId(), "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(EventIds.ExtractZipFileFailed.ToEventId(), "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Extracting zip file {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        private void CreateExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {

            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    _logger.LogInformation(EventIds.ZipFileCreationStarted.ToEventId(), "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    _fileSystemHelper.CreateZipFile(Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), Path.Combine(downloadPath, file.FileName), true);

                    _logger.LogInformation(EventIds.ZipFileCreationCompleted.ToEventId(), "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(EventIds.ZipFileCreationFailed.ToEventId(), "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Creating zip file {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        private void CreateIsoAndSha1ForExchangeSet(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    _logger.LogInformation(EventIds.CreateIsoAndSha1Started.ToEventId(), "Creating ISO and Sha1 file of {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                    _fileSystemHelper.CreateIsoAndSha1(Path.Combine(downloadPath, fileNameWithoutExtension + ".iso"), Path.Combine(downloadPath, fileNameWithoutExtension), file.VolumeIdentifier);

                    _logger.LogInformation(EventIds.CreateIsoAndSha1Completed.ToEventId(), "Creating ISO and Sha1 file of {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(EventIds.CreateIsoAndSha1Failed.ToEventId(), "Creating ISO and Sha1 file of {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Creating ISO and Sha1 file of {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        private async Task<bool> CreatePosBatch(string downloadPath, string fileExtension, Batch batchType)
        {
            string batchId = await _fssService.CreateBatch(batchType);
            IEnumerable<string> filePaths = _fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);
            UploadBatchFiles(filePaths, batchId, batchType);
            bool isCommitted = await _fssService.CommitBatch(batchId, filePaths, batchType);

            return isCommitted;
        }

        private void UploadBatchFiles(IEnumerable<string> filePaths, string batchId, Batch batchType)
        {
            Parallel.ForEach(filePaths, filePath =>
            {
                IFileInfo fileInfo = _fileSystemHelper.GetFileInfo(filePath);

                bool isFileAdded = _fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length, _mimeTypes.ContainsKey(fileInfo.Extension.ToLower()) ? _mimeTypes[fileInfo.Extension.ToLower()] : DEFAULTMIMETYPE, batchType).Result;
                if (isFileAdded)
                {
                    List<string> blockIds = _fssService.UploadBlocks(batchId, fileInfo).Result;
                    if (blockIds.Count > 0)
                    {
                        bool fileWritten = _fssService.WriteBlockFile(batchId, fileInfo.Name, blockIds).Result;
                    }
                }
            });
        }

        private async Task DownloadAioAncillaryFilesAsync(string batchId)
        {
            _logger.LogInformation(EventIds.AioAncillaryFilesDownloadStarted.ToEventId(), "Downloading of AIO base exchange set ancillary files started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);

            IEnumerable<BatchFile> fileDetails = await _fssService.GetAioInfoFolderFilesAsync(batchId, CommonHelper.CorrelationID.ToString());

            if (fileDetails != null && fileDetails.Any())
            {
                string weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();
                string aioInfoFolderPath = string.Format(_configuration["AIOAdditionalContentFilePath"], weekNumber, DateTime.UtcNow.ToString("yy"));
                string aioExchangeSetInfoPath = Path.Combine(_homeDirectoryPath, batchId, aioInfoFolderPath);

                Parallel.ForEach(fileDetails, file =>
                {
                    _fssService.DownloadFileAsync(file.Filename, file.Links.Get.Href, file.FileSize, Path.Combine(aioExchangeSetInfoPath, file.Filename)).Wait();
                });
            }
            else
            {
                _logger.LogInformation(EventIds.AioAncillaryFilesNotFound.ToEventId(), "Downloading of AIO base exchange set ancillary files not found | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            _logger.LogInformation(EventIds.AioAncillaryFilesDownloadCompleted.ToEventId(), "Downloading of AIO base exchange set ancillary files completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
        }

        private ProductVersionsRequest GetTheLatestUpdateNumber(string filePath, string[] aioCellNames)
        {
            string weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();
            string aioInfoFolderPath = string.Format(_configuration["AioUpdateZipFileName"], weekNumber, DateTime.UtcNow.ToString("yy"));
            string aioExchangeSetInfoPath = Path.Combine(filePath, Path.GetFileNameWithoutExtension(aioInfoFolderPath));

            ProductVersionsRequest productVersionsRequest = new();
            productVersionsRequest.ProductVersions = new();

            foreach (var aioCellName in aioCellNames)
            {
                var files = _fileSystemHelper.GetProductVersionsFromDirectory(aioExchangeSetInfoPath, aioCellName);

                productVersionsRequest.ProductVersions.AddRange(files);
            }
            return productVersionsRequest;
        }


        private void LogProductVersions(ProductVersionsRequest productVersionsRequest)
        {
            try
            {
                _logger.LogInformation(EventIds.LoggingProductVersionsStarted.ToEventId(), "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                _azureTableStorageHelper.SaveProductVersionDetails(productVersionsRequest.ProductVersions);

                _logger.LogInformation(EventIds.LoggingProductVersionsCompleted.ToEventId(), "Logging product version completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.LoggingProductVersionsFailed.ToEventId(), "Logging product version failed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                throw new Exception($"Logging Product version failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
            }
        }

        private List<ProductVersion> GetProductVersionsFromEntities(List<ProductVersionEntities> productVersionEntities, string[] aioCellNames)
        {
            List<ProductVersion> productVersions = new();

            foreach (var item in aioCellNames)
            {
                ProductVersion productVersion = new();

                var result = productVersionEntities.Where(p => p.ProductName == item);

                if (result != null && result.Count() > 0)
                {
                    productVersion.ProductName = result.FirstOrDefault().ProductName;
                    productVersion.EditionNumber = result.FirstOrDefault().EditionNumber;
                    productVersion.UpdateNumber = result.FirstOrDefault().UpdateNumber;
                }
                else
                {
                    productVersion.ProductName = item;
                    productVersion.EditionNumber = 0;
                    productVersion.UpdateNumber = 0;
                }
                productVersions.Add(productVersion);
            }

            return productVersions;
        }
    }
}

