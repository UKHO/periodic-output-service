using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IEssService
    {
        Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers);
        Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime);

        Task<ExchangeSetResponseModel?> GetProductDataProductVersions(ProductVersionsRequest productVersionsRequest);
    }
}
