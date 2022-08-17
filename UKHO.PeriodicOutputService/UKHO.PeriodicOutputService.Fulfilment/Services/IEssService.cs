using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IEssService
    {
        Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers);
        Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime);
    }
}
