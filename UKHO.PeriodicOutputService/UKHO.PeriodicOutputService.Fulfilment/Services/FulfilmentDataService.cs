using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

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

        public async Task<string> CreatePosExchangeSet()
        {
            Models.FleetMangerGetAuthTokenResponse tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                Models.FleetManagerGetCatalogueResponse catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {

                    //Full AVCS Batch
                    Models.ExchangeSetResponseModel response = await _essService.PostProductIdentifiersData(catalogueResponse.ProductIdentifiers);

                    string essBatchId = "cc4a0527-f82e-4355-affb-707e83293fe2";

                    //string essBatchId = CommonHelper.ExtractBatchId(url);

                    FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId);

                    if (fssBatchStatus == FssBatchStatus.Committed)
                    {
                        GetBatchResponseModel batchDetail = await _fssService.GetBatchDetails(essBatchId);

                        if (batchDetail != null)
                        {
                            var fileDetails = batchDetail.Files.Select(a => new { a.Filename, a.Links.Get.Href }).ToList();

                            string downloadPath = Path.Combine(@"D:\HOME", essBatchId);

                            _fileSystemHelper.CreateDirectory(downloadPath);

                            Parallel.ForEach(fileDetails, file =>
                            {
                                DownloadService(downloadPath, file.Filename, file.Href);
                            });

                            Parallel.ForEach(fileDetails, file =>
                            {
                                ExtractExchangeSetZip(file.Filename, downloadPath);
                            });

                            Parallel.ForEach(fileDetails, file =>
                            {
                                CreateIsoAndSha1ForExchangeSet(Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.Filename) + ".iso"), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.Filename)));
                            });


                            //Upload ISO and SHA1 files in Batch 1
                            List<string> batch1extensionsToSearch = new() { "iso", "sha1" };
                            IEnumerable<string> batch1filePaths = _fileSystemHelper.GetFiles(downloadPath, batch1extensionsToSearch, SearchOption.TopDirectoryOnly);

                            string batch1Id = await CreateBatchAndUpload(batch1filePaths);

                            bool isBatch1Committed = await _fssService.CommitBatch(batch1Id, batch1filePaths);


                            //Upload zip files in Batch 2

                            List<string> batch2extensionsToSearch = new() { "zip" };
                            IEnumerable<string> batch2filePaths = _fileSystemHelper.GetFiles(downloadPath, batch2extensionsToSearch, SearchOption.TopDirectoryOnly);

                            string batch2Id = await CreateBatchAndUpload(batch2filePaths);

                            bool isBacth2Committed = await _fssService.CommitBatch(batch2Id, batch2filePaths);
                        }
                    }
                    return "Success";
                }
            }
            return "Fail";
        }

        private void DownloadService(string downloadPath, string fileName, string href)
        {
            try
            {
                string filePath = Path.Combine(downloadPath, fileName);

                Stream stream = _fssService.DownloadFile(downloadPath, fileName, href).Result;

                byte[] bytes = _fileSystemHelper.ConvertStreamToByteArray(stream);

                _fileSystemHelper.CreateFileCopy(filePath, new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", fileName, ex.Message, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
        }

        private void ExtractExchangeSetZip(string fileName, string downloadPath)
        {
            try
            {
                _fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, fileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(fileName)), true);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", fileName, ex.Message, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
        }

        private void CreateIsoAndSha1ForExchangeSet(string directoryPath, string targetPath)
        {
            try
            {
                _fileSystemHelper.CreateIsoAndSha1(directoryPath, targetPath);

            }
            catch (Exception ex)
            {
                _logger.LogError("Create ISO and SHA1 failed");
            }
        }

        private async Task<string> CreateBatchAndUpload(IEnumerable<string> filePaths)
        {
            try
            {
                //Create Batch
                string batchId = await _fssService.CreateBatch();

                //Add files in batch created above

                Parallel.ForEach(filePaths, filePath =>
                {
                    AddAndUploadFiles(batchId, filePath);
                });

                return batchId;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void AddAndUploadFiles(string batchId, string filePath)
        {
            try
            {
                FileInfo fileInfo = _fileSystemHelper.GetFileInfo(filePath);

                bool isFileAdded = _fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length).Result;

                if (isFileAdded)
                {
                    List<string> blockIds = _fssService.UploadBlocks(batchId, fileInfo).Result;

                    if (blockIds.Count > 0)
                    {
                        bool fileWritten = _fssService.WriteBlockFile(batchId, fileInfo.Name, blockIds).Result;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}

