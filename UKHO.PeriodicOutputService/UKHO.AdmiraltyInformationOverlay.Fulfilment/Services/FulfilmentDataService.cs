using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.AdmiraltyInformationOverlay.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IFssService _fssService;
        private readonly ILogger<FulfilmentDataService> _logger;
        private readonly IConfiguration _configuration;

        private readonly string _homeDirectoryPath;

        public FulfilmentDataService(IFileSystemHelper fileSystemHelper,
                                     IFssService fssService,
                                     ILogger<FulfilmentDataService> logger,
                                     IConfiguration configuration)
        {
            _fileSystemHelper = fileSystemHelper;
            _fssService = fssService;
            _logger = logger;
            _configuration = configuration;

            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["AIOFolderName"]);
        }

        public async Task<bool> CreateAioExchangeSets()
        {
            await CreateAioBaseExchangeSet();

            return true;
        }

        private async Task CreateAioBaseExchangeSet()
        {
            _logger.LogInformation(EventIds.AioBaseExchangeSetCreationStarted.ToEventId(), "Creation of AIO base exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            //Temporary Code
            string weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();
            string aioInfoFolderPath = string.Format(_configuration["AIOAdditionalContentFilePath"], weekNumber, DateTime.UtcNow.ToString("yy"));
            string aioExchangeSetInfoPath = Path.Combine(_homeDirectoryPath, aioInfoFolderPath);
            _fileSystemHelper.CreateDirectory(aioExchangeSetInfoPath);
            //Temporary Code

            await DownloadAioAncillaryFiles(CommonHelper.CorrelationID.ToString(), aioExchangeSetInfoPath);

            _logger.LogInformation(EventIds.AioBaseExchangeSetCreationCompleted.ToEventId(), "Creation of AIO base exchange set completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
        }

        private async Task DownloadAioAncillaryFiles(string batchId, string aioExchangeSetInfoPath)
        {
            _logger.LogInformation(EventIds.AioAncillaryFilesDownloadStarted.ToEventId(), "Downloading of AIO base exchange set ancillary files started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);

            IEnumerable<BatchFile> fileDetails = await _fssService.GetAioInfoFolderFiles(batchId, CommonHelper.CorrelationID.ToString());

            if (fileDetails != null && fileDetails.Any())
            {
                foreach (BatchFile file in fileDetails)
                {
                    await _fssService.DownloadFile(file.Filename, file.Links.Get.Href, file.FileSize, Path.Combine(aioExchangeSetInfoPath, file.Filename));
                }
            }
            else
            {
                _logger.LogInformation(EventIds.AioAncillaryFilesNotFound.ToEventId(), "Downloading of AIO base exchange set ancillary files not found | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            _logger.LogInformation(EventIds.AioAncillaryFilesDownloadCompleted.ToEventId(), "Downloading of AIO base exchange set ancillary files completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
        }
    }
}
