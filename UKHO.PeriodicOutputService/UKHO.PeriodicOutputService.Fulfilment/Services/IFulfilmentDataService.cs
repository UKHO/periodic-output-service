namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFulfilmentDataService
    {
        Task<bool> CreatePosExchangeSets();
    }
}
