using System.IO.Abstractions;
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
            try
            {
                var fullAVCSExchangeSetTask = Task.Run(() => CreateFullAVCSExchangeSet());
                var updateAVCSExchangeSetTask = Task.Run(() => CreateUpdateExchangeSet());

                await Task.WhenAll(fullAVCSExchangeSetTask, updateAVCSExchangeSetTask);

                return "success";
            }
            catch (Exception)
            {
                return "fail";
                throw;
            }
        }

        private async Task CreateFullAVCSExchangeSet()
        {
            _logger.LogInformation(EventIds.PosFulfilmentJobStarted.ToEventId(), "Creation of full AVCS exchange set started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            try
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
                            var files = await GetBatchFiles(essBatchId);
                            string downloadPath = Path.Combine(@"D:\HOME", essBatchId);
                            _fileSystemHelper.CreateDirectory(downloadPath);

                            List<string> extensions = new() { "iso;sha1", "zip" };

                            foreach (string? extension in extensions)
                            {
                                IEnumerable<string> filePaths = _fileSystemHelper.GetFiles(downloadPath, extension, SearchOption.TopDirectoryOnly);
                                string batchId = await CreateBatchAndUpload(filePaths);
                                bool isBatch1Committed = await _fssService.CommitBatch(batchId, filePaths);
                            }
                        }
                        else
                        {
                            _logger.LogError("FSS polling cut off time completed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
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

                if (batchFiles.Any(f => f.FileName.Contains("error")))
                {
                    _logger.LogError("ESS exchange set creation failed.");
                    return null;
                }
            }
            return batchFiles;
        }

        private async Task<List<string>> GetFleetManagerProductIdentifiers()
        {
            List<string> productIdentifiers = new();

            FleetMangerGetAuthTokenResponseModel tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                FleetManagerGetCatalogueResponseModel catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

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
            return "cc4a0527-f82e-4355-affb-707e83293fe2";
            ////return CommonHelper.ExtractBatchId(response.Links.ExchangeSetBatchDetailsUri.ToString());
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
            catch (Exception)
            {
                _logger.LogError("Create Batch and Upload files failed");
                throw;
            }
        }

        private void AddAndUploadFiles(string batchId, string filePath)
        {
            try
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
            }
            catch (Exception)
            {
                _logger.LogError("Add and upload file failed");
                throw;
            }
        }
    }
}
