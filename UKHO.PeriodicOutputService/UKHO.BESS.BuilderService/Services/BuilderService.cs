using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.Services
{
    public class BuilderService : IBuilderService
    {
        private readonly IEssService essService;
        private readonly IFssService fssService;
        private readonly IConfiguration configuration;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly ILogger<BuilderService> logger;

        private readonly string homeDirectoryPath;

        public BuilderService(IEssService essService, IFssService fssService, IConfiguration configuration, IFileSystemHelper fileSystemHelper, ILogger<BuilderService> logger)
        {
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
            this.fssService = fssService ?? throw new ArgumentNullException(nameof(fssService));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            homeDirectoryPath = Path.Combine(configuration["HOME"]);
        }

        public async Task<string> CreateBespokeExchangeSet(ConfigQueueMessage configQueueMessage)
        {
            await RequestAndDownloadExchangeSet(configQueueMessage);
            return "Exchange Set Created Successfully";
        }

        private async Task RequestAndDownloadExchangeSet(ConfigQueueMessage configQueueMessage)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = new();
            if (configQueueMessage.Type == BessType.BASE.ToString())
                exchangeSetResponseModel = await essService.PostProductIdentifiersData((List<string>)configQueueMessage.EncCellNames, configQueueMessage.ExchangeSetStandard);
            else if (configQueueMessage.Type == BessType.UPDATE.ToString() ||
                     configQueueMessage.Type == BessType.CHANGE.ToString())
            {
                ProductVersionsRequest productVersionsRequest = GetProductVersionDetails(configQueueMessage.EncCellNames);
                exchangeSetResponseModel = await essService.GetProductDataProductVersions(productVersionsRequest, configQueueMessage.ExchangeSetStandard);
            }

            string essBatchId = CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);

            (string essFileDownloadPath, List<FssBatchFile> essFiles) =
                await DownloadEssExchangeSet(essBatchId, Batch.BesBaseZipBatch);
        }

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSet(string essBatchId, Batch batchType)
        {
            string downloadPath = Path.Combine(homeDirectoryPath, essBatchId);
            List<FssBatchFile> files = new();

            if (!string.IsNullOrEmpty(essBatchId))
            {
                FssBatchStatus fssBatchStatus = await fssService.CheckIfBatchCommitted(essBatchId, RequestType.BESS);

                if (fssBatchStatus == FssBatchStatus.Committed)
                {
                    fileSystemHelper.CreateDirectory(downloadPath);
                    files = await GetBatchFiles(essBatchId);
                    DownloadFiles(files, downloadPath);
                }
                else
                {
                    logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), fssBatchStatus, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
                }
            }
            return (downloadPath, files);
        }

        private ProductVersionsRequest GetProductVersionDetails(IEnumerable<string> encCellNames)
        {
            ProductVersionsRequest request = new();
            foreach (string item in encCellNames)
            {
                request.ProductVersions.Add(new ProductVersion { ProductName = item, EditionNumber = 0, UpdateNumber = 0 });
            }
            return request;
        }

        private async Task<List<FssBatchFile>> GetBatchFiles(string essBatchId)
        {
            GetBatchResponseModel batchDetail = await fssService.GetBatchDetails(essBatchId);
            List<FssBatchFile> batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href, FileSize = a.FileSize }).ToList();

            if (!batchFiles.Any() || batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
            {
                logger.LogError(EventIds.ErrorFileFoundInBatch.ToEventId(), "Either no files found or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.UtcNow, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.ErrorFileFoundInBatch.ToEventId());
            }
            return batchFiles;
        }

        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                fssService.DownloadFileAsync(file.FileName, file.FileLink, file.FileSize, filePath).Wait();
            });
        }
    }
}
