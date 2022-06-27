using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFleetManagerService
    {
        Task<FleetMangerGetAuthTokenResponse> GetJwtAuthUnpToken();
        Task<FleetManagerGetCatalogueResponse> GetCatalogue(string accessToken);
    }
}
