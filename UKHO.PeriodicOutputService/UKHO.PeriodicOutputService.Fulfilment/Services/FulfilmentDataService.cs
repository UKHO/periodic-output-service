using System.IO.Abstractions;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fm.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IEssService _essService;
        private readonly IFssService _fssService;
        private readonly ILogger<FulfilmentDataService> _logger;
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IAzureTableStorageHelper _azureTableStorageHelper;
        private readonly IConfiguration _configuration;

        private const string FULLAVCSISOSHA1EXCHANGESETFILEEXTENSION = "iso;sha1";
        private const string FULLAVCSZIPEXCHANGESETFILEEXTENSION = "zip";
        private const string UPDATEZIPEXCHANGESETFILEEXTENSION = "zip";
        private const string CATALOGUEFILEEXTENSION = "xml";
        private const string ENCUPDATELISTFILEEXTENSION = "csv";
        private const string ESSVALIDATIONREASONFORCANCELLEDPRODUCT = "noDataAvailableForCancelledProduct";

        private readonly Dictionary<string, string> mimeTypes = new()
        {
            { ".zip", "application/zip" },
            { ".xml", "text/xml" },
            { ".csv", "text/csv" },
            { ".iso", "application/x-raw-disk-image" },
            { ".sha1", "text/plain" }
        };
        private readonly string DEFAULTMIMETYPE = "application/octet-stream";

        private readonly string _homeDirectoryPath;

        private DateTime? _nextSchedule;

        private ITransaction _currentTransaction => Agent.Tracer.CurrentTransaction;

        private readonly string _aioFileName;

        public FulfilmentDataService(IFleetManagerService fleetManagerService,
                                     IEssService exchangeSetApiService,
                                     IFssService fssService,
                                     IFileSystemHelper fileSystemHelper,
                                     ILogger<FulfilmentDataService> logger,
                                     IConfiguration configuration,
                                     IAzureTableStorageHelper azureTableStorageHelper)
        {
            _fleetManagerService = fleetManagerService;
            _essService = exchangeSetApiService;
            _fssService = fssService;
            _fileSystemHelper = fileSystemHelper;
            _logger = logger;
            _configuration = configuration;
            _azureTableStorageHelper = azureTableStorageHelper;
            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["POSFolderName"]);
            _aioFileName = _configuration["AIOFileName"] ?? string.Empty;
        }

        public async Task<bool> CreatePosExchangeSets()
        {
            _fileSystemHelper.CreateDirectory(_homeDirectoryPath);

            _logger.LogInformation(EventIds.GetLatestSinceDateTimeStarted.ToEventId(), "Getting latest since datetime started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            DateTime sinceDateTime = _azureTableStorageHelper.GetSinceDateTime();

            _logger.LogInformation(EventIds.GetLatestSinceDateTimeCompleted.ToEventId(), "Getting latest since datetime completed  | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            bool isSuccess = false;

            try
            {
                Task[] tasks = null;

                Task fullAVCSExchangeSetTask = Task.Run(() => CreateFullAVCSExchangeSet());
                Task updateAVCSExchangeSetTask = Task.Run(() => CreateUpdateExchangeSet(sinceDateTime.ToString("R")));

                tasks = new Task[] { fullAVCSExchangeSetTask, updateAVCSExchangeSetTask };

                await Task.WhenAll(tasks);

                isSuccess = true;

                return isSuccess;
            }
            finally
            {
                if (!bool.Parse(_configuration["IsFTRunning"] ?? bool.FalseString))
                {
                    LogHistory(_nextSchedule ?? sinceDateTime, isSuccess);
                }
            }
        }

        private async Task CreateFullAVCSExchangeSet()
        {
            _logger.LogInformation(EventIds.FullAvcsExchangeSetCreationStarted.ToEventId(), "Creation of full AVCS exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var isFullAvcsDvdBatchCreated = false;
            var isFullAvcsZipBatchCreated = false;
            var isCatalogueFileBatchCreated = false;
            var isEncUpdateFileBatchCreated = false;

            var span = _currentTransaction?.StartSpan("FullAVCSExchangeSet", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                var productIdentifiers = await GetFleetManagerProductIdentifiers();
                var essBatchId = await PostProductIdentifiersToESS(productIdentifiers);
                (var essFileDownloadPath, var essFiles) = await DownloadEssExchangeSet(essBatchId, Batch.EssFullAvcsZipBatch);

                if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
                {
                    ExtractExchangeSetZip(essFiles, essFileDownloadPath);
                    CreateIsoAndSha1ForExchangeSet(essFiles, essFileDownloadPath);

                    isFullAvcsDvdBatchCreated = await CreatePosBatch(essFileDownloadPath, FULLAVCSISOSHA1EXCHANGESETFILEEXTENSION, Batch.PosFullAvcsIsoSha1Batch);
                    isFullAvcsZipBatchCreated = await CreatePosBatch(essFileDownloadPath, FULLAVCSZIPEXCHANGESETFILEEXTENSION, Batch.PosFullAvcsZipBatch);

                    if (isFullAvcsDvdBatchCreated && isFullAvcsZipBatchCreated)
                    {
                        _logger.LogInformation(EventIds.FullAvcsExchangeSetCreationCompleted.ToEventId(),
                            "Full AVCS exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                            DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                        isCatalogueFileBatchCreated = await CreatePosBatch(_homeDirectoryPath, CATALOGUEFILEEXTENSION, Batch.PosCatalogueBatch);

                        if (isCatalogueFileBatchCreated)
                        {
                            _logger.LogInformation(EventIds.BatchCreationForCatalogueCompleted.ToEventId(),
                                "Batch for catalougue created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                                DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                        }

                        var weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);
                        var encUpdateListFilePath = Path.Combine(essFileDownloadPath, string.Format(_configuration["EncUpdateListFilePath"], weekNumber.Week, weekNumber.YearShort));
                        isEncUpdateFileBatchCreated = await CreatePosBatch(encUpdateListFilePath, ENCUPDATELISTFILEEXTENSION, Batch.PosEncUpdateBatch);

                        if (isEncUpdateFileBatchCreated)
                        {
                            _logger.LogInformation(EventIds.BatchCreationForENCUpdateCompleted.ToEventId(),
                                "Batch for ENC updates created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                                DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                        }
                    }
                }
                else
                {
                    _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(),
                        "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                        DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
                }
            }
            catch (Exception e)
            {
                span?.CaptureException(e);
                throw;
            }
            finally
            {
                _currentTransaction?.SetLabel("FullAvcsDvdBatchCreated", isFullAvcsDvdBatchCreated);
                _currentTransaction?.SetLabel("FullAvcsZipBatchCreated", isFullAvcsZipBatchCreated);
                _currentTransaction?.SetLabel("CatalogueFileBatchCreated", isCatalogueFileBatchCreated);
                _currentTransaction?.SetLabel("EncUpdateFileBatchCreated", isEncUpdateFileBatchCreated);
                span?.End();
            }
        }

        private async Task CreateUpdateExchangeSet(string sinceDateTime)
        {
            _logger.LogInformation(EventIds.UpdateExchangeSetCreationStarted.ToEventId(), "Creation of update exchange set for SinceDateTime - {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            bool isUpdateZipBatchCreated = false;

            ISpan span = _currentTransaction?.StartSpan("UpdateExchangeSet", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                string essBatchId = await GetProductDataSinceDateTimeFromEss(sinceDateTime);

                (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSet(essBatchId, Batch.EssUpdateZipBatch);

                if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
                {
                    isUpdateZipBatchCreated = await CreatePosBatch(essFileDownloadPath, UPDATEZIPEXCHANGESETFILEEXTENSION, Batch.PosUpdateBatch);

                    if (isUpdateZipBatchCreated)
                    {
                        _logger.LogInformation(EventIds.UpdateExchangeSetCreationCompleted.ToEventId(), "Update exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    }
                }
                else
                {
                    _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(), "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
                }
            }
            catch (Exception e)
            {
                span?.CaptureException(e);
                throw;
            }
            finally
            {
                _currentTransaction?.SetLabel("UpdateZipBatchCreated", isUpdateZipBatchCreated);
                span?.End();
            }
        }

        private async Task<List<string>> GetFleetManagerProductIdentifiers()
        {
            FleetMangerGetAuthTokenResponseModel tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();
            FleetManagerGetCatalogueResponseModel catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);
            return catalogueResponse.ProductIdentifiers;
        }

        private async Task<string> PostProductIdentifiersToESS(List<string> productIdentifiers)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = await _essService.PostProductIdentifiersData(productIdentifiers);

            if (exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Any())
            {
                if (exchangeSetResponseModel.RequestedProductsNotInExchangeSet.All(p => p.Reason == ESSVALIDATIONREASONFORCANCELLEDPRODUCT))
                {
                    _logger.LogInformation(EventIds.CancelledProductsFound.ToEventId(), "{Count} cancelled products found while creating full avcs exchange set and they are [{Products}] on  {DateTime} | _X-Correlation-ID : {CorrelationId}", exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), string.Join(',', exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Select(a => a.ProductName).ToList()), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                else
                {
                    _logger.LogError(EventIds.EssValidationFailed.ToEventId(), "ESS validation failed for {Count} products [{Products}] while creating full avcs exchange set {DateTime} | _X-Correlation-ID : {CorrelationId}", exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), string.Join(',', exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Select(a => a.ProductName + " - " + a.Reason).ToList()), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.EssValidationFailed.ToEventId());
                }
            }

            string essBatchId = CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
            _logger.LogInformation(EventIds.BatchCreatedInESS.ToEventId(), "Batch for Full AVCS exchange set created by ESS successfully with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return essBatchId;
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
                    throw;
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
                    throw;
                }
            });
        }

        private async Task<string> GetProductDataSinceDateTimeFromEss(string sinceDateTime)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = await _essService.GetProductDataSinceDateTime(sinceDateTime);

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

            _nextSchedule = exchangeSetResponseModel.ResponseDateTime.AddMinutes(-5);

            return essBatchId;
        }

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSet(string essBatchId, Batch batchType)
        {
            string downloadPath = Path.Combine(_homeDirectoryPath, essBatchId);
            List<FssBatchFile> encFiles = new();

            if (!string.IsNullOrEmpty(essBatchId))
            {
                FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId, RequestType.POS);

                if (fssBatchStatus == FssBatchStatus.Committed)
                {
                    _fileSystemHelper.CreateDirectory(downloadPath);
                    List<FssBatchFile> batchFiles = await GetBatchFiles(essBatchId);

                    // exclude AIO file 
                    encFiles = batchFiles.Where(f => !f.FileName.ToLower().Equals(_aioFileName.ToLower())).ToList();

                    DownloadFiles(encFiles, downloadPath);

                    encFiles = RenameFiles(downloadPath, encFiles, batchType);
                }
                else
                {
                    _logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), fssBatchStatus, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
                }
            }
            return (downloadPath, encFiles);
        }

        private List<FssBatchFile> RenameFiles(string downloadPath, List<FssBatchFile> files, Batch batchType)
        {
            foreach (var file in files)
            {
                var fileInfo = _fileSystemHelper.GetFileInfo(Path.Combine(downloadPath, file.FileName));
                var weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);

                if (batchType == Batch.EssFullAvcsZipBatch)
                {
                    var dvdNumber = int.Parse(file.FileName.Substring(1, 2));
                    file.FileName = string.Format(_configuration["PosAvcsZipFileName"], dvdNumber, weekNumber.Week, weekNumber.YearShort);
                    file.VolumeIdentifier = string.Format(_configuration["PosDVDVolumeIdentifier"], dvdNumber);
                }
                else
                {
                    file.FileName = string.Format(_configuration["PosUpdateZipFileName"], weekNumber.Week, weekNumber.YearShort);
                }

                fileInfo.MoveTo(Path.Combine(downloadPath, file.FileName));
            }

            return files;
        }

        private async Task<List<FssBatchFile>> GetBatchFiles(string essBatchId)
        {
            List<FssBatchFile> batchFiles = null;

            GetBatchResponseModel batchDetail = await _fssService.GetBatchDetails(essBatchId);
            batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href, FileSize = a.FileSize }).ToList();

            if (!batchFiles.Any() || batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
            {
                _logger.LogError(EventIds.ErrorFileFoundInBatch.ToEventId(), "Either no files found or error file found in batch with BathcID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.ErrorFileFoundInBatch.ToEventId());
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

        private async Task<bool> CreatePosBatch(string downloadPath, string fileExtension, Batch batchType)
        {
            var isCommitted = false;
            var span = _currentTransaction?.StartSpan("CreatePOSBatch", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                var weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow);
                var batchId = await _fssService.CreateBatch(batchType, weekNumber);
                var filePaths = _fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);
                UploadBatchFiles(filePaths, batchId, batchType);
                isCommitted = await _fssService.CommitBatch(batchId, filePaths, batchType);
            }
            finally
            {
                span?.End();
            }

            return isCommitted;
        }

        private void UploadBatchFiles(IEnumerable<string> filePaths, string batchId, Batch batchType)
        {
            Parallel.ForEach(filePaths, filePath =>
           {
               IFileInfo fileInfo = _fileSystemHelper.GetFileInfo(filePath);

               bool isFileAdded = _fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length, mimeTypes.ContainsKey(fileInfo.Extension.ToLower()) ? mimeTypes[fileInfo.Extension.ToLower()] : DEFAULTMIMETYPE, batchType).Result;
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

        private void LogHistory(DateTime nextSchedule, bool isSuccess)
        {
            try
            {
                _logger.LogInformation(EventIds.LoggingHistoryStarted.ToEventId(), "Logging history started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                long invertedTimeKey = DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks;

                WebJobHistory webJobHistory = new()
                {
                    PartitionKey = DateTime.UtcNow.ToString("MMyyyy"),
                    RowKey = invertedTimeKey.ToString(),
                    IsJobSuccess = isSuccess,
                    SinceDateTime = nextSchedule
                };
                _azureTableStorageHelper.SaveHistory(webJobHistory);
                _logger.LogInformation(EventIds.LoggingHistoryCompleted.ToEventId(), "Logging history completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            }
            catch (Exception)
            {
                _logger.LogInformation(EventIds.LoggingHistoryFailed.ToEventId(), "Logging history failed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                throw;

            }

        }
    }
}
