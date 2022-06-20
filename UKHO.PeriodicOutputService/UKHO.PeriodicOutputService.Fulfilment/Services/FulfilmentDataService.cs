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
            string? unpAccessToken = _fleetManagerService.GetJwtAuthUnpToken().Result.AuthToken;
            
            if (!string.IsNullOrEmpty(unpAccessToken))
            {
                string? jwtAccessToken = _fleetManagerService.GetJwtAuthJwtToken(unpAccessToken).Result.AuthToken;
                if (!string.IsNullOrEmpty(jwtAccessToken))
                {
                    await Task.Delay(2000);
                    List<string>? productIdentifiers = _fleetManagerService.GetCatalogue(jwtAccessToken).Result.ProductIdentifiers;

                    return "Fleet Manager full AVCS ProductIdentifiers received";
                }
            }
            return "Fleet Manager full AVCS ProductIdentifiers not received";
        }
    }
}
