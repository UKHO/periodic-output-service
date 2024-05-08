namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAuthPksTokenProvider
    {
        public Task<string> GetManagedIdentityAuthForPksAsync(string resource);
    }
}
