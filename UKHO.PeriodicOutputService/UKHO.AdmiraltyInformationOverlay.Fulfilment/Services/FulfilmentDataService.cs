﻿using System.IO.Abstractions;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;
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
        private readonly IOptions<FssApiConfiguration> _fssApiConfig;

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

        private ITransaction _currentTransaction => Agent.Tracer.CurrentTransaction;

        public FulfilmentDataService(IFileSystemHelper fileSystemHelper,
                                     IEssService essService,
                                     IFssService fssService,
                                     ILogger<FulfilmentDataService> logger,
                                     IConfiguration configuration,
                                     IAzureTableStorageHelper azureTableStorageHelper,
                                     IOptions<FssApiConfiguration> fssApiConfig)
        {
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _essService = essService ?? throw new ArgumentNullException(nameof(essService));
            _fssService = fssService ?? throw new ArgumentNullException(nameof(fssService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));

            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["AIOFolderName"]);
            _fssApiConfig = fssApiConfig ?? throw new ArgumentNullException(nameof(fssApiConfig));
        }

        public async Task<bool> CreateAioExchangeSetsAsync()
        {
            _fileSystemHelper.CreateDirectory(_homeDirectoryPath);
            var isSuccess = false;

            _logger.LogInformation(EventIds.GetLatestProductVersionDetailsStarted.ToEventId(), "Getting latest product version details started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var productVersionEntities = _azureTableStorageHelper.GetLatestProductVersionDetails();

            _logger.LogInformation(EventIds.GetLatestProductVersionDetailsCompleted.ToEventId(), "Getting latest product version details completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow, GetWeeksToIncrement());
            var aioBaseExchangeSetTask = Task.Run(() => CreateAioBaseExchangeSet(weekNumber));
            var updateAioExchangeSetTask = Task.Run(() => CreateUpdateAIOExchangeSet(productVersionEntities, weekNumber));

            var tasks = new Task[] { aioBaseExchangeSetTask, updateAioExchangeSetTask };

            await Task.WhenAll(tasks);

            isSuccess = true;

            return isSuccess;
        }

        private int GetWeeksToIncrement()
        {
            var aioJobConfigurationEntities = _azureTableStorageHelper.GetAioJobConfiguration();
            var weeksToIncrement = aioJobConfigurationEntities?.WeeksToIncrement ?? int.Parse(_configuration["WeeksToIncrement"]);
            return weeksToIncrement;
        }

        private async Task CreateAioBaseExchangeSet(FormattedWeekNumber weekNumber)
        {
            _logger.LogInformation(EventIds.AioBaseExchangeSetCreationStarted.ToEventId(), "Creation of AIO base exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var isFullAvcsDvdBatchCreated = false;
            var span = _currentTransaction?.StartSpan("AioBaseExchangeSet", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                if (!string.IsNullOrEmpty(_configuration["AioCells"]))
                {
                    var aioCells = Convert.ToString(_configuration["AioCells"]).Split(',').ToList();
                    var essBatchId = await PostProductIdentifiersToESS(aioCells);
                    (var essFileDownloadPath, var essFiles) = await DownloadEssExchangeSet(essBatchId, Batch.EssAioBaseZipBatch, weekNumber);

                    if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
                    {
                        ExtractExchangeSetZip(essFiles, essFileDownloadPath);
                        await DownloadAioAncillaryFilesAsync(essBatchId, weekNumber);
                        UpdateSerialAioFile(essFiles, essFileDownloadPath, "BASE", weekNumber);
                        CreateExchangeSetZip(essFiles, essFileDownloadPath);
                        CreateIsoAndSha1ForExchangeSet(essFiles, essFileDownloadPath);
                        isFullAvcsDvdBatchCreated = await CreatePosBatch(essFileDownloadPath, AIOBASEZIPISOSHA1EXCHANGESETFILEEXTENSION, Batch.AioBaseCDZipIsoSha1Batch, weekNumber);
                    }
                    else
                    {
                        _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(),
                            "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                            DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                        throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
                    }
                }
                else
                {
                    _logger.LogError(EventIds.AioCellsConfigurationMissing.ToEventId(),
                        "AIO cells are empty in configuration | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                        DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.AioCellsConfigurationMissing.ToEventId());
                }
            }
            catch (Exception e)
            {
                span?.CaptureException(e);
                throw;
            }
            finally
            {
                _currentTransaction?.SetLabel("AIOFullAvcsDvdBatchCreated", isFullAvcsDvdBatchCreated);
                span?.End();
            }

            _logger.LogInformation(EventIds.AioBaseExchangeSetCreationCompleted.ToEventId(), "Creation of AIO base exchange set completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
        }

        private async Task CreateUpdateAIOExchangeSet(List<ProductVersionEntities> productVersionEntities, FormattedWeekNumber weekNumber)
        {
            _logger.LogInformation(EventIds.AioUpdateExchangeSetCreationStarted.ToEventId(), "Creation of update exchange set for Productversions - {Productversions} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", productVersionEntities, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var isUpdateZipBatchCreated = false;
            var span = _currentTransaction?.StartSpan("UpdateExchangeSet", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                var aioCellNames = Convert.ToString(_configuration["AioCells"]).Split(',').ToArray();
                var productVersions = GetProductVersionsFromEntities(productVersionEntities, aioCellNames);
                var essBatchId = await GetProductDataVersionFromEss(new ProductVersionsRequest { ProductVersions = productVersions });
                (var essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSet(essBatchId, Batch.EssUpdateZipBatch, weekNumber);

                if (!string.IsNullOrEmpty(essFileDownloadPath) && essFiles.Count > 0)
                {
                    ExtractExchangeSetZip(essFiles, essFileDownloadPath);
                    UpdateSerialAioFile(essFiles, essFileDownloadPath, "UPDATE", weekNumber);
                    var latestProductVersions = GetTheLatestUpdateNumber(essFileDownloadPath, aioCellNames, weekNumber);
                    CreateExchangeSetZip(essFiles, essFileDownloadPath);
                    isUpdateZipBatchCreated = await CreatePosBatch(essFileDownloadPath, UPDATEZIPEXCHANGESETFILEEXTENSION, Batch.AioUpdateZipBatch, weekNumber);

                    if (isUpdateZipBatchCreated)
                    {
                        _logger.LogInformation(EventIds.AioUpdateExchangeSetCreationCompleted.ToEventId(),
                            "Update exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}",
                            DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                        if (!bool.Parse(_configuration["IsFTRunning"]))
                        {
                            LogProductVersions(latestProductVersions);
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
                _currentTransaction?.SetLabel("AIOUpdateZipBatchCreated", isUpdateZipBatchCreated);
                span?.End();
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

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSet(string essBatchId, Batch batchType, FormattedWeekNumber weekNumber)
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

                    files = RenameFiles(downloadPath, files, batchType, weekNumber);
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

        private List<FssBatchFile> RenameFiles(string downloadPath, List<FssBatchFile> files, Batch batchType, FormattedWeekNumber weekNumber)
        {
            foreach (var file in files)
            {
                var fileInfo = _fileSystemHelper.GetFileInfo(Path.Combine(downloadPath, file.FileName));
                file.VolumeIdentifier = "V01X01";

                if (batchType == Batch.EssAioBaseZipBatch)
                {
                    file.FileName = string.Format(_configuration["AioBaseZipFileName"], weekNumber.Week, weekNumber.YearShort);
                }
                else
                {
                    file.FileName = string.Format(_configuration["AioUpdateZipFileName"], weekNumber.Week, weekNumber.YearShort);
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

        private async Task<bool> CreatePosBatch(string downloadPath, string fileExtension, Batch batchType, FormattedWeekNumber weekNumber)
        {
            var isCommitted = false;
            var span = _currentTransaction?.StartSpan("CreateAIOBatch", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
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

        private async Task DownloadAioAncillaryFilesAsync(string batchId, FormattedWeekNumber weekNumber)
        {
            _logger.LogInformation(EventIds.AioAncillaryFilesDownloadStarted.ToEventId(), "Downloading of AIO base exchange set ancillary files started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);

            var fileDetails = await _fssService.GetAioInfoFolderFilesAsync(batchId, CommonHelper.CorrelationID.ToString());

            if (fileDetails != null && fileDetails.Any())
            {
                var aioInfoFolderPath = string.Format(_configuration["AIOAdditionalContentFilePath"], weekNumber.Week, weekNumber.YearShort);
                var aioExchangeSetInfoPath = Path.Combine(_homeDirectoryPath, batchId, aioInfoFolderPath);

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

        /// <summary>
        /// Update the content of serial.aio file to fix
        /// https://dev.azure.com/ukhydro/Abzu/_workitems/edit/220298
        /// </summary>
        /// <param name="fileDetails"></param>
        /// <param name="downloadPath"></param>
        /// <param name="cdType"></param>
        /// <param name="weekNumber"></param>
        /// <returns></returns>
        /// <exception cref="FulfilmentException"></exception>
        private void UpdateSerialAioFile(List<FssBatchFile> fileDetails, string downloadPath, string cdType, FormattedWeekNumber weekNumber)
        {
            var serialFileName = _fssApiConfig.Value.SerialFileName;
           
            Parallel.ForEach(fileDetails, file =>
            {
                var serialFilePath = Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName), serialFileName);

                try
                {
                   if (File.Exists(serialFilePath))
                   {
                       var serialFileContent = $"GBWK{weekNumber.Week}-{DateTime.UtcNow:yy}   {DateTime.UtcNow.Year:D4}{DateTime.UtcNow.Month:D2}{DateTime.UtcNow.Day:D2}{cdType,-10}{2:D2}.00\x0b\x0d\x0a";

                       _fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);

                       _logger.LogInformation(EventIds.SerialAioUpdated.ToEventId(), "SERIAL.AIO file at {serialFilePath} updated with week number {weekNumber} | _X-Correlation-ID:{CorrelationId}", serialFilePath, weekNumber.Week, CommonHelper.CorrelationID);
                   }   
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(EventIds.SerialAioUpdateFailed.ToEventId(), "SERIAL.AIO file update operation failed at {DateTime} | {ErrorMessage} | {serialFilePath} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), ex.Message, serialFilePath, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.SerialAioUpdateFailed.ToEventId());
                }
            });
        }

        private ProductVersionsRequest GetTheLatestUpdateNumber(string filePath, string[] aioCellNames, FormattedWeekNumber weekNumber)
        {
            var aioInfoFolderPath = string.Format(_configuration["AioUpdateZipFileName"], weekNumber.Week, weekNumber.YearShort);
            var aioExchangeSetInfoPath = Path.Combine(filePath, Path.GetFileNameWithoutExtension(aioInfoFolderPath));
            var productVersionsRequest = new ProductVersionsRequest { ProductVersions = [] };

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

