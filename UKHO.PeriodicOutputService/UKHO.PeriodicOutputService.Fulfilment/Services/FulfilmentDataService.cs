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

        public FulfilmentDataService(IFleetManagerService fleetManagerService,
                                     IEssService exchangeSetApiService,
                                     IFssService fssService,
                                     IFileSystemHelper fileSystemHelper,
                                     ILogger<FulfilmentDataService> logger)
        {
            _fleetManagerService = fleetManagerService;
            _essService = exchangeSetApiService;
            _fssService = fssService;
            _fileSystemHelper = fileSystemHelper;
            _logger = logger;
        }

        public async Task<string> CreatePosExchangeSet()
        {
            Models.FleetMangerGetAuthTokenResponse tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                Models.FleetManagerGetCatalogueResponse catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    Models.ExchangeSetResponseModel response = await _essService.PostProductIdentifiersData(catalogueResponse.ProductIdentifiers);

                    string batchId = "621E8D6F-9950-4BA6-BFB4-92415369AAEE";

                    //string batchId = CommonHelper.ExtractBatchId(url);

                    FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(batchId);

                    if (fssBatchStatus == FssBatchStatus.Committed)
                    {
                        GetBatchResponseModel batchDetail = await _fssService.GetBatchDetails(batchId);

                        if (batchDetail != null)
                        {
                            var fileDetails = batchDetail.Files.Select(a => new { a.Filename, a.Links.Get.Href }).ToList();

                            string downloadPath = Path.Combine(@"D:\HOME", batchId);

                            _fileSystemHelper.CreateDirectory(downloadPath);

                            List<Task> ParallelDownloadFileTasks = new() { };

                            Parallel.ForEach(fileDetails, file =>
                            {
                                ParallelDownloadFileTasks.Add(DownloadService(downloadPath, file.Filename, file.Href));
                            });

                            Task.WaitAll(ParallelDownloadFileTasks.ToArray());
                            ParallelDownloadFileTasks.Clear();

                            List<Task> ParallelExtractFileTasks = new();

                            Parallel.ForEach(fileDetails, file =>
                            {
                                ParallelExtractFileTasks.Add(ExtractExchangeSetZip(file.Filename, downloadPath));
                            });

                            Task.WaitAll(ParallelExtractFileTasks.ToArray());
                            ParallelExtractFileTasks.Clear();

                            List<Task> ParallelIsoAndSha1FileTasks = new();

                            Parallel.ForEach(fileDetails, file =>
                            {
                                ParallelIsoAndSha1FileTasks.Add(CreateIsoAndSha1ForExchangeSet(Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.Filename) + ".iso"), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.Filename))));
                            });

                            Task.WaitAll(ParallelIsoAndSha1FileTasks.ToArray());
                            ParallelIsoAndSha1FileTasks.Clear();

                            IEnumerable<string> filePaths = _fileSystemHelper.GetFiles(downloadPath, "*.iso;*.sha1", SearchOption.TopDirectoryOnly);

                            bool result = await CreateBatchAndUpload(filePaths);

                            if (result)
                            {
                                
                            }
                        }
                    }
                    return "Success";
                }
            }
            return "Fail";
        }

        private async Task<bool> DownloadService(string downloadPath, string fileName, string href)
        {
            try
            {
                string filePath = Path.Combine(downloadPath, fileName);

                Stream stream = await _fssService.DownloadFile(downloadPath, fileName, href);

                byte[] bytes = _fileSystemHelper.ConvertStreamToByteArray(stream);

                _fileSystemHelper.CreateFileCopy(filePath, new MemoryStream(bytes));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", fileName, ex.Message, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return false;
            }
        }

        private async Task<bool> ExtractExchangeSetZip(string filename, string downloadPath)
        {
            try
            {
                _fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, filename), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(filename)), true);

                await Task.CompletedTask;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> CreateIsoAndSha1ForExchangeSet(string directoryPath, string targetPath)
        {
            try
            {
                _fileSystemHelper.CreateIsoAndSha1(directoryPath, targetPath);

                await Task.CompletedTask;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> CreateBatchAndUpload(IEnumerable<string> filePaths)
        {
            try
            {
                //Create Batch

                string batchId = await _fssService.CreateBatch();

                //Add files in batch created above
                List<Task> ParallelUploadFileToBatchTasks = new() { };

                Parallel.ForEach(filePaths, filePath =>
                {
                    ParallelUploadFileToBatchTasks.Add(AddAndUploadFiles(batchId, filePath));
                });

                Task.WaitAll(ParallelUploadFileToBatchTasks.ToArray());
                ParallelUploadFileToBatchTasks.Clear();

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task AddAndUploadFiles(string batchId, string filePath)
        {
            try
            {
                FileInfo fileInfo = _fileSystemHelper.GetFileInfo(filePath);

                bool isFileAdded = await _fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length);

                if (isFileAdded)
                {
                    List<string> blockIds = await _fssService.UploadBlocks(batchId, fileInfo);

                    if (blockIds.Count > 0)
                    {
                        await _fssService.WriteBlockFile(batchId, fileInfo.Name, blockIds);
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

