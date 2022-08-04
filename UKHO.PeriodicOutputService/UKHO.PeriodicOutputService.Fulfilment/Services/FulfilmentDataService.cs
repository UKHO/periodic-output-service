using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss;
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

        public async Task<string> CreatePosExchangeSets()
        {
            var fullAVCSExchangeSetTask = Task.Run(() => CreateFullAVCSExchangeSet());
            var updateAVCSExchangeSetTask = Task.Run(() => CreateUpdateExchangeSet());

            await Task.WhenAll(fullAVCSExchangeSetTask, updateAVCSExchangeSetTask);
            return "success";
        }

        private async Task CreateFullAVCSExchangeSet()
        {
            List<string> productIdentifiers = await GetFleetManagerProductIdentifiers();

            if (productIdentifiers.Count <= 0)
            {
                _logger.LogError("Product identifiers not found");
            }
            else
            {
                string essBatchId = await PostProductIdentifiersToESS(productIdentifiers);

                if (!string.IsNullOrEmpty(essBatchId))
                {
                    FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(essBatchId);

                    if (fssBatchStatus == FssBatchStatus.Committed)
                    {
                        List<FssBatchFile>? files = await GetBatchFiles(essBatchId);

                        if (files != null)
                        {
                            string downloadPath = Path.Combine(@"D:\HOME", essBatchId);
                            _fileSystemHelper.CreateDirectory(downloadPath);

                            DownloadFiles(files, downloadPath);
                        }
                        else
                        {
                            _logger.LogError("ESS exchange set creation failed.");
                        }
                    }
                    else
                    {
                        _logger.LogError("FSS polling cut off time completed");
                    }
                }
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

            if (batchDetail != null)
            {
                batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href }).ToList();

                if (batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
                {
                    return null;
                }
            }
            return batchFiles;
        }

        private async Task<List<string>> GetFleetManagerProductIdentifiers()
        {
            List<string> productIdentifiers = new();

            Models.FleetMangerGetAuthTokenResponseModel tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                Models.FleetManagerGetCatalogueResponseModel catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    productIdentifiers = catalogueResponse.ProductIdentifiers;
                }
            }
            return productIdentifiers;
        }

        private async Task<string> PostProductIdentifiersToESS(List<string> productIdentifiers)
        {
            Models.ExchangeSetResponseModel response = await _essService.PostProductIdentifiersData(productIdentifiers);
            return CommonHelper.ExtractBatchId(response.Links.ExchangeSetBatchDetailsUri.Href);
        }

        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {

                string filePath = Path.Combine(downloadPath, file.FileName);
                Stream stream = _fssService.DownloadFile(downloadPath, file.FileName, file.FileLink).Result;
                byte[] bytes = _fileSystemHelper.ConvertStreamToByteArray(stream);
                _fileSystemHelper.CreateFileCopy(filePath, new MemoryStream(bytes));

            });
        }
    }
}
