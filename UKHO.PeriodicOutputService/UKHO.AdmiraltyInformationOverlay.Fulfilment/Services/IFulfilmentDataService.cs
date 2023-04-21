namespace UKHO.AdmiraltyInformationOverlay.Fulfilment.Services
{
    public interface IFulfilmentDataService
    {
        Task<bool> CreateAioExchangeSets();
    }
}
