using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.Torus.Core;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class Catalog031Helper : ICatalog031Helper
    {
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IOptions<FssApiConfiguration> _fssApiConfig;
        private readonly IFactory<ICatalog031Builder> _catalog031BuilderFactory;
        private readonly ICatalog031ReaderFactory _catalog031ReaderFactory;
        private readonly ILogger<Catalog031Helper> _logger;

        private const string CATALOGFILENAME = "CATALOG.031";

        public Catalog031Helper(IFileSystemHelper fileSystemHelper, IOptions<FssApiConfiguration> fssApiConfig, IFactory<ICatalog031Builder> catalog031BuilderFactory, ICatalog031ReaderFactory catalog031ReaderFactory, ILogger<Catalog031Helper> logger)
        {
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _fssApiConfig = fssApiConfig ?? throw new ArgumentNullException(nameof(fssApiConfig));
            _catalog031BuilderFactory = catalog031BuilderFactory ?? throw new ArgumentNullException(nameof(catalog031BuilderFactory));
            _catalog031ReaderFactory = catalog031ReaderFactory ?? throw new ArgumentNullException(nameof(catalog031ReaderFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RemoveReadmeEntryAndUpdateCatalogFile(string catalogFilePath)
        {
            catalogFilePath = Path.Combine(catalogFilePath, CATALOGFILENAME);
            try
            {
                _logger.LogInformation(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessStarted.ToEventId(), "Starting the process of removing README entry and updating catalog file. | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                var catalogEntries = GetCatalogEntries(catalogFilePath);
                var updatedCatalogBytes = FilterAndWriteCatalogEntries(catalogEntries);
                ReplaceCatalogFile(catalogFilePath, updatedCatalogBytes);

                _logger.LogInformation(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessCompleted.ToEventId(), "Successfully completed the process of removing README entry and updating catalog for file. | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessFailed.ToEventId(), "An error occurred while processing catalog file. | ErrorMessage: {ErrorMessage} | _X-Correlation-ID: {CorrelationId}", ex.Message, CommonHelper.CorrelationID);
                throw;
            }
        }

        private IEnumerable<CatalogEntry> GetCatalogEntries(string catalogFilePath)
        {
            byte[] catalogFileBytes = File.ReadAllBytes(catalogFilePath);
            return _catalog031ReaderFactory.Create(catalogFileBytes).ReadCatalogue();
        }

        private byte[] FilterAndWriteCatalogEntries(IEnumerable<CatalogEntry> catalogEntries)
        {
            var catBuilder = _catalog031BuilderFactory.Create();

            foreach (var catalogEntry in catalogEntries)
            {
                if (!catalogEntry.FileLocation.Equals(_fssApiConfig.Value.ReadMeFileName) && !catalogEntry.FileLocation.Equals(CATALOGFILENAME))
                {
                    catBuilder.Add(catalogEntry);
                }
            }

            return catBuilder.WriteCatalog(_fssApiConfig.Value.BespokeExchangeSetFileFolder);
        }

        private void ReplaceCatalogFile(string catalogFilePath, byte[] updatedCatalogBytes)
        {
            _fileSystemHelper.DeleteFile(catalogFilePath);
            using (var memoryStream = new MemoryStream(updatedCatalogBytes))
            {
                _fileSystemHelper.CreateFileCopy(catalogFilePath, memoryStream);
            }
        }
    }
}
