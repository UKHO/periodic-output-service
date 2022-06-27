using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;

        public FulfilmentDataService(IFleetManagerService fleetManagerService)
        {
            _fleetManagerService = fleetManagerService;
        }

        public async Task<string> CreatePosExchangeSet()
        {
            FleetMangerGetAuthTokenResponse tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                FleetManagerGetCatalogueResponse catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                    return "Fleet Manager full AVCS ProductIdentifiers received";
            }
            return "Fleet Manager full AVCS ProductIdentifiers not received";
        }
    }
}
