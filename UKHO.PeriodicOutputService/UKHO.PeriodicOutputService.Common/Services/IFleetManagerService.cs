using UKHO.PeriodicOutputService.Common.Models.Fm.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IFleetManagerService
    {
        Task<FleetMangerGetAuthTokenResponseModel> GetJwtAuthUnpToken();
        Task<FleetManagerGetCatalogueResponseModel> GetCatalogue(string accessToken);
    }
}
