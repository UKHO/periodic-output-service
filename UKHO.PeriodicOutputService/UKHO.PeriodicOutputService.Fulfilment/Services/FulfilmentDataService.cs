using System.IO.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using DiscUtils.Iso9660;
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
            string sinceDateTime = DateTime.UtcNow.AddDays(-7).ToString("R");

            var fullAVCSExchangeSetTask = Task.Run(() => CreateFullAVCSExchangeSet());
            var updateAVCSExchangeSetTask = Task.Run(() => CreateUpdateExchangeSet(sinceDateTime));

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

                    //start - temporary code to extract and create iso sha1 files. Actula refined code is in another branch.
                    foreach (var file in files)
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                        ZipFile.ExtractToDirectory(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);
                        IEnumerable<string> srcFiles = Directory.EnumerateFiles(Path.Combine(downloadPath, fileNameWithoutExtension), "*.*", SearchOption.AllDirectories);
                        CreateIsoAndSha1(srcFiles, Path.Combine(downloadPath, fileNameWithoutExtension + ".iso"), Path.Combine(downloadPath, fileNameWithoutExtension));
                    }
                    //end - temporary code to extract and create iso sha1 files. Actula refined code is in another branch.

                    var extensions = new List<(string fileExtension, string mediaType)> { ("iso;sha1", "DVD"), ("zip", "Zip") };

                    foreach ((string fileExtension, string mediaType) extension in extensions)
                    {
                        IEnumerable<string> filePaths = _fileSystemHelper.GetFiles(downloadPath, extension.fileExtension, SearchOption.TopDirectoryOnly);
                        string batchId = await CreateBatchAndUploadFiles(filePaths, extension.mediaType);
                        bool isCommitted = await _fssService.CommitBatch(batchId, filePaths);
                        if (isCommitted)
                        {
                            _logger.LogInformation(EventIds.FullAvcsExchangeSetCreationCompleted.ToEventId(), "Full AVCS exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                        }
                    }
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

        private async Task CreateUpdateExchangeSet(string sinceDateTime)
        {
            _logger.LogInformation(EventIds.UpdateExchangeSetCreationStarted.ToEventId(), "Creation of update exchange for SinceDateTime - {SinceDateTime} set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string essBatchId = await GetProductDataSinceDateTimeFromEss(sinceDateTime);

            if (!string.IsNullOrEmpty(essBatchId))
            {
                FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId);

                if (fssBatchStatus == FssBatchStatus.Committed)
                {
                    List<FssBatchFile>? files = await GetBatchFiles(essBatchId);

                    string downloadPath = Path.Combine(_configuration["HOME"], essBatchId);

                    _fileSystemHelper.CreateDirectory(downloadPath);

                    DownloadFiles(files, downloadPath);
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

        private async Task<string> GetProductDataSinceDateTimeFromEss(string sinceDateTime)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = await _essService.GetProductDataSinceDateTime(sinceDateTime);
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
            Parallel.ForEach(fileDetails, new ParallelOptions { MaxDegreeOfParallelism = 4 }, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                Stream stream = _fssService.DownloadFile(file.FileName, file.FileLink).Result;
                _fileSystemHelper.CreateFileCopy(filePath, stream);
            });
        }

        private async Task<string> CreateBatchAndUploadFiles(IEnumerable<string> filePaths, string mediaType)
        {
            string batchId = await _fssService.CreateBatch(mediaType);

            Parallel.ForEach(filePaths, new ParallelOptions { MaxDegreeOfParallelism = 4 }, filePath =>
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
            return batchId;
        }


        //start - temporary code to extract and create iso sha1 files. Actula refined code is in another branch.
        private void CreateIsoAndSha1(IEnumerable<string> srcFiles, string targetPath, string directoryPath)
        {
            var iso = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = "FullAVCSExchangeSet"
            };

            foreach (string? file in srcFiles)
            {
                var fi = new FileInfo(file);
                if (fi.Directory.Name == directoryPath)
                {
                    iso.AddFile($"{fi.Name}", fi.FullName);
                    continue;
                }
                string? srcDir = fi.Directory.FullName.Replace(directoryPath, "").TrimEnd('\\');
                iso.AddDirectory(srcDir);
                iso.AddFile($"{srcDir}\\{fi.Name}", fi.FullName);
            }
            iso.Build(targetPath);

            byte[] isoFileBytes = System.Text.Encoding.UTF8.GetBytes(targetPath);
            string hash = BitConverter.ToString(SHA1.Create().ComputeHash(isoFileBytes)).Replace("-", "");
            File.WriteAllText(targetPath + ".sha1", hash);
        }
        //end - temporary code to extract and create iso sha1 files. Actula refined code is in another branch.
    }
}
