using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.Torus.Core;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class Catalog031FilterHelper : ICatalog031FilterHelper
    {
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IOptions<FssApiConfiguration> _fssApiConfig;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Catalog031FilterHelper> _logger;
        private readonly IFactory<ICatalog031Builder> _catalog031BuilderFactory;
        private readonly ICatalog031ReaderFactory _catalog031ReaderFactory;

        private const string EXCHANGSETCATALOGFILE = "ExchangeSetCatalogFileName";

        public Catalog031FilterHelper(IFileSystemHelper fileSystemHelper, IOptions<FssApiConfiguration> fssApiConfig, IConfiguration configuration, ILogger<Catalog031FilterHelper> logger, IFactory<ICatalog031Builder> catalog031BuilderFactory, ICatalog031ReaderFactory catalog031ReaderFactory)
        {
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _fssApiConfig = fssApiConfig ?? throw new ArgumentNullException(nameof(fssApiConfig));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _catalog031BuilderFactory = catalog031BuilderFactory ??
                                        throw new ArgumentNullException(nameof(catalog031BuilderFactory));
            _catalog031ReaderFactory = catalog031ReaderFactory ??
                                       throw new ArgumentNullException(nameof(catalog031ReaderFactory));
        }

        public void RemoveReadmeEntryAndUpdateCatalog(string catalogFilePath)
        {
            try
            {
                _logger.LogInformation(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessStarted.ToEventId(), "Starting the process of removing README entry and updating catalog for file:| {CatalogFilePath} | {DateTime} | _X-Correlation-ID : {CorrelationId}", catalogFilePath, DateTime.UtcNow, CommonHelper.CorrelationID);
                byte[] catalogFileBytes = File.ReadAllBytes(catalogFilePath);
                var details = _catalog031ReaderFactory.Create(catalogFileBytes).ReadCatalogue();
                var catBuilder = _catalog031BuilderFactory.Create();
                foreach (var lst in details)
                {
                    if (!lst.FileLocation.Equals(_fssApiConfig.Value.ReadMeFileName) &&
                        !lst.FileLocation.Equals(_configuration[EXCHANGSETCATALOGFILE]!))
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
                _logger.LogInformation(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessCompleted.ToEventId(), "Successfully completed the process of removing README entry and updating catalog for file:| {CatalogFilePath} | {DateTime} | _X-Correlation-ID : {CorrelationId}", catalogFilePath, DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessFailed.ToEventId(), "An error occurred while processing catalog file: {CatalogFilePath} at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}", catalogFilePath, DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
