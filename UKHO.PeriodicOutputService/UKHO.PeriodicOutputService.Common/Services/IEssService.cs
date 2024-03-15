using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IEssService
    {
        Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers, string? exchangeSetStandard = null);
        Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime, string? exchangeSetStandard = null);
        Task<ExchangeSetResponseModel?> GetProductDataProductVersions(ProductVersionsRequest productVersionsRequest, string? exchangeSetStandard = null);
    }
}
