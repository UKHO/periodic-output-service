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
            string? unpAccessToken = _fleetManagerService.GetJwtAuthUnpToken().Result.AuthToken;

            if (!string.IsNullOrEmpty(unpAccessToken))
            {
                List<string>? productIdentifiers = _fleetManagerService.GetCatalogue(unpAccessToken).Result.ProductIdentifiers;

                if (productIdentifiers != null)
                {
                    var response = _exchangeSetApiService.GetProductIdentifiersData(productIdentifiers).Result;
                }
                return "Fleet Manager full AVCS ProductIdentifiers received";
            }
            return "Fleet Manager full AVCS ProductIdentifiers not received";
        }
    }
}
