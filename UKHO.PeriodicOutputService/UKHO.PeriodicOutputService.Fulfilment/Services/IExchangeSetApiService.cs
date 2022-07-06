using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IExchangeSetApiService
    {
        Task<ExchangeSetGetBatchResponse> GetProductIdentifiersData(List<string> productIdentifiers);
    }
}
