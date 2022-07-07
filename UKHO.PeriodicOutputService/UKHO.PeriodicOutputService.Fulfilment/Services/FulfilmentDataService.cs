namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IExchangeSetApiService _exchangeSetApiService;

        public FulfilmentDataService(IFleetManagerService fleetManagerService, IExchangeSetApiService exchangeSetApiService)
        {
            _fleetManagerService = fleetManagerService;
            _exchangeSetApiService = exchangeSetApiService;
        }

        public async Task<string> CreatePosExchangeSet()
        {
            var tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                return "Fleet Manager full AVCS ProductIdentifiers not received";
            }

            var catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

            if (catalogueResponse.ProductIdentifiers == null || catalogueResponse.ProductIdentifiers.Count <= 0)
            {
                return "Fleet Manager full AVCS ProductIdentifiers not received";
            }

            var exchangeSetGetBatchResponse = await _exchangeSetApiService.GetProductIdentifiersData(catalogueResponse.ProductIdentifiers);
            return "Fleet Manager full AVCS ProductIdentifiers received";
        }
    }
}
