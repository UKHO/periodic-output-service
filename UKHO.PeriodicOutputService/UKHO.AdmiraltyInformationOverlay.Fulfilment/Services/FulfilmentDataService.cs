using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
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

        private readonly string _homeDirectoryPath;
        private const string ESSVALIDATIONREASONFORCANCELLEDPRODUCT = "noDataAvailableForCancelledProduct";
        private const string AIOBASEZIPISOSHA1EXCHANGESETFILEEXTENSION = "zip;iso;sha1";
        private readonly Dictionary<string, string> mimeTypes = new()
        {
            { ".zip", "application/zip" },
            { ".iso", "application/x-raw-disk-image" },
            { ".sha1", "text/plain" }
        };

        private readonly string DEFAULTMIMETYPE = "application/octet-stream";

        public FulfilmentDataService(IFileSystemHelper fileSystemHelper,
                                     IEssService essService,
                                     IFssService fssService,
                                     ILogger<FulfilmentDataService> logger,
                                     IConfiguration configuration)
        {
            _fileSystemHelper = fileSystemHelper;
            _essService = essService;
            _fssService = fssService;
            _logger = logger;
            _configuration = configuration;

            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["AIOFolderName"]);
        }

        public async Task<bool> CreateAioExchangeSetsAsync()
        {
            _fileSystemHelper.CreateDirectory(_homeDirectoryPath);

            bool isSuccess = false;

            await CreateAioBaseExchangeSet();

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

        private async Task DownloadAioAncillaryFilesAsync(string batchId)
        {
            _logger.LogInformation(EventIds.AioAncillaryFilesDownloadStarted.ToEventId(), "Downloading of AIO base exchange set ancillary files started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);

            string weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();
            string aioInfoFolderPath = string.Format(_configuration["AIOAdditionalContentFilePath"], weekNumber, DateTime.UtcNow.ToString("yy"));
            string aioExchangeSetInfoPath = Path.Combine(_homeDirectoryPath, batchId, aioInfoFolderPath);

            IEnumerable<BatchFile> fileDetails = await _fssService.GetAioInfoFolderFilesAsync(batchId, CommonHelper.CorrelationID.ToString());

            if (fileDetails != null && fileDetails.Any())
            {
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
    }
}

