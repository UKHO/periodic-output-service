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
        }

        public async Task<string> CreatePosExchangeSets()
        {
            var fullAVCSExchangeSetTask = Task.Run(() => CreateFullAVCSExchangeSet());
            var updateAVCSExchangeSetTask = Task.Run(() => CreateUpdateExchangeSet());

            await Task.WhenAll(fullAVCSExchangeSetTask, updateAVCSExchangeSetTask);
            return "success";
        }

        private async Task CreateFullAVCSExchangeSet()
        {
            _logger.LogInformation(EventIds.FullAvcsExchangeSetCreationStarted.ToEventId(), "Creation of full AVCS exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            List<string> productIdentifiers = await GetFleetManagerProductIdentifiers();
            string essBatchId = await PostProductIdentifiersToESS(productIdentifiers);

            if (!string.IsNullOrEmpty(essBatchId))
            {
                FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId);

                if (fssBatchStatus == FssBatchStatus.Committed)
                {
                    List<FssBatchFile>? files = await GetBatchFiles(essBatchId);

                    string downloadPath = Path.Combine(_configuration["HOME"], essBatchId);

                    _fileSystemHelper.CreateDirectory(downloadPath);

                    DownloadFiles(files, downloadPath);

                    ExtractExchangeSetZip(files, downloadPath);

                    CreateIsoAndSha1ForExchangeSet(files, downloadPath);

                }
                else
                {
                    _logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), fssBatchStatus, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
                }
            }
            else
            {
                _logger.LogError(EventIds.EmptyBatchIdFound.ToEventId(), "Batch ID found empty | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.EmptyBatchIdFound.ToEventId());
            }
        }

        private async Task CreateUpdateExchangeSet()
        {
            await Task.CompletedTask;
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
            else
            {
                _logger.LogError(EventIds.FssBatchDetailUrlNotFound.ToEventId(), "FSS batch detail URL not found in ESS response at {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.FssBatchDetailUrlNotFound.ToEventId());
            }
        }

        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                Stream stream = _fssService.DownloadFile(downloadPath, file.FileName, file.FileLink).Result;
                _fileSystemHelper.CreateFileCopy(filePath, stream);
            });
        }

        private void ExtractExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    _fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, ex.Message, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
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
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                    _fileSystemHelper.CreateIsoAndSha1(Path.Combine(downloadPath, fileNameWithoutExtension + ".iso"), Path.Combine(downloadPath, fileNameWithoutExtension));
                }
                catch (Exception)
                {
                    _logger.LogError("Create ISO and SHA1 failed");
                    throw;
                }
            });
        }
    }
}
