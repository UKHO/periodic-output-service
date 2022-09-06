using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IEssService _essService;
        private readonly IFssService _fssService;
        private readonly ILogger<FulfilmentDataService> _logger;
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IConfiguration _configuration;

        private const string FULLAVCSISOSHA1EXCHANGESETFILEEXTENSION = "iso;sha1";
        private const string FULLAVCSISOSHA1EXCHANGESETMEDIATYPE = "dvd";

        private const string FULLAVCSZIPEXCHANGESETFILEEXTENSION = "zip";
        private const string FULLAVCSZIPEXCHANGESETMEDIATYPE = "zip";

        private const string UPDATEZIPEXCHANGESETFILEEXTENSION = "zip";
        private const string UPDATEZIPEXCHANGESETMEDIATYPE = "zip";
        private readonly string _homeDirectoryFolderPath = string.Empty;

        public FulfilmentDataService(IFleetManagerService fleetManagerService,
                                     IEssService exchangeSetApiService,
                                     IFssService fssService,
                                     IFileSystemHelper fileSystemHelper,
                                     ILogger<FulfilmentDataService> logger,
                                     IConfiguration configuration)
        {
            _fleetManagerService = fleetManagerService;
            _essService = exchangeSetApiService;
            _fssService = fssService;
            _fileSystemHelper = fileSystemHelper;
            _logger = logger;
            _configuration = configuration;
            _homeDirectoryFolderPath = Path.Combine(_configuration["HOME"], "POS");
        }

        public async Task<string> CreatePosExchangeSets()
        {
            try
            {
                string sinceDateTime = DateTime.UtcNow.AddDays(-7).ToString("R");

                var fullAVCSExchangeSetTask = Task.Run(() => CreateFullAVCSExchangeSet());
                var updateAVCSExchangeSetTask = Task.Run(() => CreateUpdateExchangeSet(sinceDateTime));

                await Task.WhenAll(fullAVCSExchangeSetTask, updateAVCSExchangeSetTask);

                return "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing Periodic Output Service webjob at {DateTime} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.UnhandledException.ToEventId());
            }
            finally
            {
                //This will first delete directory then create same new directory 
                _fileSystemHelper.CreateDirectory(_homeDirectoryFolderPath);
            }
        }

        private async Task CreateFullAVCSExchangeSet()
        {
            _logger.LogInformation(EventIds.FullAvcsExchangeSetCreationStarted.ToEventId(), "Creation of full AVCS exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            List<string> productIdentifiers = await GetFleetManagerProductIdentifiers();

            string essBatchId = await PostProductIdentifiersToESS(productIdentifiers);

            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSet(essBatchId);

            if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
            {
                ExtractExchangeSetZip(essFiles, essFileDownloadPath);

                CreateIsoAndSha1ForExchangeSet(essFiles, essFileDownloadPath);

                bool isFullAvcsDvdBatchCreated = await CreatePosBatch(essFileDownloadPath, FULLAVCSISOSHA1EXCHANGESETFILEEXTENSION, FULLAVCSISOSHA1EXCHANGESETMEDIATYPE, Batch.PosFullAvcsIsoSha1Batch);
                bool isFullAvcsZipBatchCreated = await CreatePosBatch(essFileDownloadPath, FULLAVCSZIPEXCHANGESETFILEEXTENSION, FULLAVCSZIPEXCHANGESETMEDIATYPE, Batch.PosFullAvcsZipBatch);

                if (isFullAvcsDvdBatchCreated && isFullAvcsZipBatchCreated)
                {
                    _logger.LogInformation(EventIds.FullAvcsExchangeSetCreationCompleted.ToEventId(), "Full AVCS exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
            }
            else
            {
                _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(), "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
            }
        }

        private async Task CreateUpdateExchangeSet(string sinceDateTime)
        {
            _logger.LogInformation(EventIds.UpdateExchangeSetCreationStarted.ToEventId(), "Creation of update exchange set for SinceDateTime - {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string essBatchId = await GetProductDataSinceDateTimeFromEss(sinceDateTime);

            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSet(essBatchId);

            if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
            {
                bool isUpdateZipBatchCreated = await CreatePosBatch(essFileDownloadPath, UPDATEZIPEXCHANGESETFILEEXTENSION, UPDATEZIPEXCHANGESETMEDIATYPE, Batch.PosUpdateBatch);

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

        private async Task<List<string>> GetFleetManagerProductIdentifiers()
        {
            FleetMangerGetAuthTokenResponseModel tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();
            FleetManagerGetCatalogueResponseModel catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);
            return catalogueResponse.ProductIdentifiers;
        }

        private async Task<string> PostProductIdentifiersToESS(List<string> productIdentifiers)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = await _essService.PostProductIdentifiersData(productIdentifiers);

            if (!string.IsNullOrEmpty(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href))
            {
                string essBatchId = CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
                _logger.LogInformation(EventIds.BatchCreatedInESS.ToEventId(), "Batch is created by ESS successfully with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return essBatchId;
            }

            _logger.LogError(EventIds.FssBatchDetailUrlNotFound.ToEventId(), "FSS batch detail URL not found in ESS response at {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            throw new FulfilmentException(EventIds.FssBatchDetailUrlNotFound.ToEventId());
        }

        private void ExtractExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    _logger.LogInformation(EventIds.ExtractZipFileStarted.ToEventId(), "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    _fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);

                    _logger.LogInformation(EventIds.ExtractZipFileCompleted.ToEventId(), "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

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
                    _logger.LogInformation(EventIds.CreateIsoAndSha1Started.ToEventId(), "Creating ISO and Sha1 file of {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                    _fileSystemHelper.CreateIsoAndSha1(Path.Combine(downloadPath, fileNameWithoutExtension + ".iso"), Path.Combine(downloadPath, fileNameWithoutExtension));

                    _logger.LogInformation(EventIds.CreateIsoAndSha1Completed.ToEventId(), "Creating ISO and Sha1 file of {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

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
            if (!string.IsNullOrEmpty(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href))
            {
                string essBatchId = CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
                _logger.LogInformation(EventIds.BatchCreatedInESS.ToEventId(), "Batch is created by ESS successfully with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", essBatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                return essBatchId;
            }

            _logger.LogError(EventIds.FssBatchDetailUrlNotFound.ToEventId(), "FSS batch detail URL not found in ESS response at {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            throw new FulfilmentException(EventIds.FssBatchDetailUrlNotFound.ToEventId());
        }

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSet(string essBatchId)
        {
            string downloadPath = Path.Combine(_homeDirectoryFolderPath, essBatchId);
            List<FssBatchFile> files = new();

            if (!string.IsNullOrEmpty(essBatchId))
            {
                FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId);

                if (fssBatchStatus == FssBatchStatus.Committed)
                {
                    _fileSystemHelper.CreateDirectory(downloadPath);
                    files = await GetBatchFiles(essBatchId);
                    DownloadFiles(files, downloadPath);
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
            batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href }).ToList();

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
                Stream stream = _fssService.DownloadFile(file.FileName, file.FileLink).Result;
                _fileSystemHelper.CreateFileCopy(filePath, stream);
            });
        }

        private async Task<bool> CreatePosBatch(string downloadPath, string fileExtension, string mediaType, Batch batchType)
        {

            string batchId = await _fssService.CreateBatch(mediaType, batchType);
            IEnumerable<string> filePaths = _fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);
            UploadBatchFiles(filePaths, batchId);
            bool isCommitted = await _fssService.CommitBatch(batchId, filePaths);

            return isCommitted;
        }

        private void UploadBatchFiles(IEnumerable<string> filePaths, string batchId)
        {
            Parallel.ForEach(filePaths, filePath =>
            {
                IFileInfo fileInfo = _fileSystemHelper.GetFileInfo(filePath);
                bool isFileAdded = _fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length).Result;
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
    }
}
