using System.Text;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;

        public FulfilmentDataService(IFleetManagerService fleetManagerService)
        {
            _fleetManagerService = fleetManagerService;
        }

        public async Task<StringBuilder> CreatePosExchangeSet()
        {
            StringBuilder catalougeXml = new();

            try
            {
                string unpAccessToken = await _fleetManagerService.GetJwtAuthUnpToken();

                if (!string.IsNullOrEmpty(unpAccessToken))
                {
                    string jwtAccessToken = await _fleetManagerService.GetJwtAuthJwtToken(unpAccessToken);
                    if (!string.IsNullOrEmpty(jwtAccessToken))
                    {
                        catalougeXml = await _fleetManagerService.GetCatalogue(jwtAccessToken);
                    }
                }

                return catalougeXml;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);

                return catalougeXml;
            }
        }
    }
}
