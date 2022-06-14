using System.Text;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFleetManagerService
    {
        Task<string> GetJwtAuthUnpToken();
        Task<string> GetJwtAuthJwtToken(string accessToken);
        Task<StringBuilder> GetCatalogue(string accessToken);
    }
}
