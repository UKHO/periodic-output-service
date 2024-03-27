using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        Task<string> ProcessConfigsAsync();

        Task<bool> CheckConfigFrequencyAndSaveQueueDetailsAsync(IList<BessConfig> bessConfigs, IList<SalesCatalogueDataProductResponse> salesCatalogueDataProducts);
    }
}
