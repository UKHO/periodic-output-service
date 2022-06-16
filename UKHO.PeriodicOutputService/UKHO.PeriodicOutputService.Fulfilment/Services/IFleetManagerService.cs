namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFleetManagerService
    {
        Task<string> GetJwtAuthUnpToken();
        Task<string> GetJwtAuthJwtToken(string accessToken);
        Task<List<string>> GetCatalogue(string accessToken);
    }
}
