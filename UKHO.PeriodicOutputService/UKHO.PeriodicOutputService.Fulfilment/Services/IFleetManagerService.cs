using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFleetManagerService
    {
        Task<FleetMangerGetAuthTokenResponseModel> GetJwtAuthUnpToken();
        Task<FleetManagerGetCatalogueResponseModel> GetCatalogue(string accessToken);
    }
}
