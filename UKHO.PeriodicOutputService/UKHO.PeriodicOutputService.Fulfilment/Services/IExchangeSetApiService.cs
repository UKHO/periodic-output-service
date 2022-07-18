using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IExchangeSetApiService
    {
        Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers);
    }
}
