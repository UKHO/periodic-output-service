using System.Net;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFleetManagerService
    {
        Task<FleetMangerGetAuthTokenResponse> GetJwtAuthUnpToken();
        Task<FleetMangerGetAuthTokenResponse> GetJwtAuthJwtToken(string accessToken);
        Task<FleetManagerGetCatalogueResponse> GetCatalogue(string accessToken);
    }
}
