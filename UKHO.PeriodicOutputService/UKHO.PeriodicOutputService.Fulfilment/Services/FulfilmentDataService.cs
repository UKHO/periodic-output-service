using UKHO.PeriodicOutputService.Fulfilment.Models;

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
            FleetMangerGetAuthTokenResponse tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                FleetManagerGetCatalogueResponse catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    var response = await _exchangeSetApiService.GetProductIdentifiersData(catalogueResponse.ProductIdentifiers);
                    return "Fleet Manager full AVCS ProductIdentifiers received";
                }
            }
            return "Fleet Manager full AVCS ProductIdentifiers not received";
        }
    }
}
