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

                            string downloadPath = Path.Combine(@"D:\\HOME", batchId);

                            _fileSystemHelper.CreateDirectory(downloadPath);

                            List<Task> ParallelDownloadFileTasks = new() { };

                            Parallel.ForEach(fileDetails, file =>
                            {
                                ParallelDownloadFileTasks.Add(DownloadService(downloadPath, file.Filename, file.Href));
                            });

                            await Task.WhenAll(ParallelDownloadFileTasks);

                            //ParallelDownloadFileTasks = new();

                            //Parallel.ForEach(fileDetails, file =>
                            //{
                            //    ParallelDownloadFileTasks.Add(ExtractExchangeSetZip(file.Filename, downloadPath));
                            //});

                            //await Task.WhenAll(ParallelDownloadFileTasks);

                            //await _fssService.CreateISOFiles();

                            IEnumerable<string> filePaths = _fileSystemHelper.GetFiles(downloadPath, "zip");

                            bool result = await CreateBatchAndUpload(filePaths);
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
                _fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, filename), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(filename)), false);

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
            //Create Batch

            string batchId = await _fssService.CreateBatch();

            //Add files in batch created above
            List<Task> ParallelUploadFileToBatchTasks = new() { };

            Parallel.ForEach(filePaths, filePath =>
            {
                ParallelUploadFileToBatchTasks.Add(AddAndUploadFiles(batchId, filePath));
            });

            await Task.WhenAll(ParallelUploadFileToBatchTasks);

            return true;
        }

        private async Task<bool> AddAndUploadFiles(string batchId, string filePath)
        {
            FileInfo fileInfo = _fileSystemHelper.GetFileInfo(filePath);

            bool isFileAdded = await _fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length);

            if (isFileAdded)
            {
                return true;
            }
            return false;
        }
    }
}
