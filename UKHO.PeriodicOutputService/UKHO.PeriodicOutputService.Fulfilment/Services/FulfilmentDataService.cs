using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IEssService _essService;
        private readonly IFssService _fssService;
        private readonly ILogger<FulfilmentDataService> _logger;

        public FulfilmentDataService(IFleetManagerService fleetManagerService,
                                     IEssService exchangeSetApiService,
                                     IFssService fssService,
                                     ILogger<FulfilmentDataService> logger)
        {
            _fleetManagerService = fleetManagerService;
            _essService = exchangeSetApiService;
            _fssService = fssService;
            _logger = logger;
        }

        public async Task<string> CreatePosExchangeSet()
        {
            var tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                var catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    var response = await _essService.PostProductIdentifiersData(catalogueResponse.ProductIdentifiers);

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

                            if (!Directory.Exists(downloadPath))
                            {
                                Directory.CreateDirectory(downloadPath);
                            }

                            List<Task> ParallelDownloadFileTasks = new List<Task> { };

                            Parallel.ForEach(fileDetails, link =>
                            {
                                ParallelDownloadFileTasks.Add(_fssService.DownloadFile(downloadPath, link.Filename, link.Href));
                            });

                            await Task.WhenAll(ParallelDownloadFileTasks);
                        }
                    }
                    return "Success";
                }
            }
            return "Fail";
        }
    }
}
