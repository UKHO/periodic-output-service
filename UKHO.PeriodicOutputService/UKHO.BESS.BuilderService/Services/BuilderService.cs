using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.Services
{
    public class BuilderService : IBuilderService
    {
        private readonly IEssService essService;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ILogger<BuilderService> logger;
        private readonly IConfiguration configuration;
        private readonly IFileSystemHelper fileSystemHelper;

        public BuilderService(IEssService essService, IAzureTableStorageHelper azureTableStorageHelper, ILogger<BuilderService> logger, IConfiguration configuration, IFileSystemHelper fileSystemHelper)
        {
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        }

        public async Task<string> CreateBespokeExchangeSet(ConfigQueueMessage message)
        {
            await essService.PostProductIdentifiersData(message.EncCellNames.ToList(), message.ExchangeSetStandard);

            //_logger.LogInformation(EventIds.GetLatestProductVersionDetailsStarted.ToEventId(), "Getting latest product version details started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var productVersionEntities = await azureTableStorageHelper.GetLatestBessProductVersionDetailsAsync();

            var productVersions = GetBessProductVersionsFromEntities(productVersionEntities, message.EncCellNames.ToArray());

            string essFileDownloadPath = "";

            var latestProductVersions = GetTheLatestUpdateNumber(essFileDownloadPath, message.EncCellNames.ToArray());

            //_logger.LogInformation(EventIds.GetLatestProductVersionDetailsCompleted.ToEventId(), "Getting latest product version details completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return "Exchange Set Created Successfully";
        }

        private List<ProductVersion> GetBessProductVersionsFromEntities(List<BessProductVersionEntities> productVersionEntities, string[] cellNames)
        {
            List<ProductVersion> productVersions = new();

            foreach (var cellName in cellNames)
            {
                ProductVersion productVersion = new();

                var result = productVersionEntities.Where(p => p.ProductName == cellName);

                if (result != null && result.Count() > 0)
                {
                    productVersion.ProductName = result.FirstOrDefault().ProductName;
                    productVersion.EditionNumber = result.FirstOrDefault().EditionNumber;
                    productVersion.UpdateNumber = result.FirstOrDefault().UpdateNumber;
                }
                else
                {
                    productVersion.ProductName = cellName;
                    productVersion.EditionNumber = 0;
                    productVersion.UpdateNumber = 0;
                }
                productVersions.Add(productVersion);
            }

            return productVersions;
        }

        private ProductVersionsRequest GetTheLatestUpdateNumber(string filePath, string[] cellNames)
        {
            string weekNumber = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();
            string aioInfoFolderPath = string.Format("BespokeESFromNameFromConfig", weekNumber, DateTime.UtcNow.ToString("yy"));
            string aioExchangeSetInfoPath = Path.Combine(filePath, Path.GetFileNameWithoutExtension(aioInfoFolderPath));

            ProductVersionsRequest productVersionsRequest = new();
            productVersionsRequest.ProductVersions = new();

            foreach (var cellName in cellNames)
            {
                var files = fileSystemHelper.GetProductVersionsFromDirectory(aioExchangeSetInfoPath, cellName);

                productVersionsRequest.ProductVersions.AddRange(files);
            }
            return productVersionsRequest;
        }
    }
}
