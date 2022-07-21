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
                        BatchDetail batchDetail = await _fssService.GetBatchDetails(batchId);

                        if (batchDetail != null)
                        {
                            var fileDetails = batchDetail.Files.Select(a => new { a.Filename, a.Links.Get.Href }).ToList();

                            string downloadPath = Path.Combine(@"D:\\HOME", batchId);

                            _fileSystemHelper.CreateDirectory(downloadPath);

                            List<Task> ParallelDownloadFileTasks = new() { };

                            Parallel.ForEach(fileDetails, link =>
                            {
                                ParallelDownloadFileTasks.Add(DownloadService(downloadPath, link.Filename, link.Href));
                            });

                            await Task.WhenAll(ParallelDownloadFileTasks);
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
            catch(Exception ex)
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", fileName, ex.Message, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return false;
            }
        }
    }
}
