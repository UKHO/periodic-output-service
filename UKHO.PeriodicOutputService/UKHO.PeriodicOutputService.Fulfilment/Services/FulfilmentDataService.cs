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
            List<string> productIdentifiers = new();
            try
            {
                string unpAccessToken = await _fleetManagerService.GetJwtAuthUnpToken();

                if (!string.IsNullOrEmpty(unpAccessToken))
                {
                    string jwtAccessToken = await _fleetManagerService.GetJwtAuthJwtToken(unpAccessToken);
                    if (!string.IsNullOrEmpty(jwtAccessToken))
                    {
                        productIdentifiers = await _fleetManagerService.GetCatalogue(jwtAccessToken);

                        return "Fleet Manager full AVCS ProductIdentifiers received";
                    }
                }
                return "Fleet Manager full AVCS ProductIdentifiers not received";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                return "Exception occured while getting full AVCS ProductIdentifiers from Fleet Manager";
            }
        }
    }
}
