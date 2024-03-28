using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        public Task<string> ProcessConfigsAsync();

        Task<bool> CheckConfigFrequencyAndSaveQueueDetails(IList<BessConfig> bessConfigs, IList<SalesCatalogueDataProductResponse> salesCatalogueDataProducts);
    }
}