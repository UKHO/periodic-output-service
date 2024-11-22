using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class Catalog031FilterHelper : ICatalog031FilterHelper
    {
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IOptions<FssApiConfiguration> _fssApiConfig;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Catalog031FilterHelper> _logger;

        private const string EXCHANGEETATALOGFILE = "ExchangeSetCatalogFileName";

        public Catalog031FilterHelper(IFileSystemHelper fileSystemHelper, IOptions<FssApiConfiguration> fssApiConfig, IConfiguration configuration, ILogger<Catalog031FilterHelper> logger)
        {
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _fssApiConfig = fssApiConfig ?? throw new ArgumentNullException(nameof(fssApiConfig));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RemoveReadmeEntryAndUpdateCatalog(string catalogFilePath)
        {
            try
            {
                _logger.LogInformation(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessStarted.ToEventId(), "Starting the process of removing README entry and updating catalog for file:| {CatalogFilePath} | {DateTime} | _X-Correlation-ID : {CorrelationId}", catalogFilePath, DateTime.UtcNow, CommonHelper.CorrelationID);
                byte[] catalogFileBytes = _fileSystemHelper.GetFileInBytes(new UploadFileBlockRequestModel
                {
                    FullFileName = catalogFilePath,
                    Length = (int)new FileInfo(catalogFilePath).Length,
                    Offset = 0
                });
                var details = new Catalog031Reader(catalogFileBytes);
                var list = details.ReadCatalogue();
                var catBuilder = new Catalog031BuilderFactory().Create();
                foreach (var lst in list)
                {
                    if (!lst.FileLocation.Equals(_fssApiConfig.Value.ReadMeFileName) &&
                        !lst.FileLocation.Equals(_configuration[EXCHANGEETATALOGFILE]!))
                    {
                        catBuilder.Add(lst);
                    }
                }
                var cat031Bytes = catBuilder.WriteCatalog(_fssApiConfig.Value.BespokeExchangeSetFileFolder);
                _fileSystemHelper.DeleteFile(catalogFilePath);
                using (var memoryStream = new MemoryStream(cat031Bytes))
                {
                    _fileSystemHelper.CreateFileCopy(catalogFilePath, memoryStream);
                }
               _logger.LogInformation(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessCompleted.ToEventId(), " Successfully completed the process of removing README entry and updating catalog for file:| {CatalogFilePath} | {DateTime} | _X-Correlation-ID : {CorrelationId}", catalogFilePath, DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessFailed.ToEventId(), "An error occurred while processing catalog file: {CatalogFilePath} at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}", catalogFilePath, DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
