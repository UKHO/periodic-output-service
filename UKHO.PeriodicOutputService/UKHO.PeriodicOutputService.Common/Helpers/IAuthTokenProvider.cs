namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAuthTokenProvider
    {
        Task<string> GetManagedIdentityAuthAsync(string essClientId);
    }
}
