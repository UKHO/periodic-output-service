namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAuthEssTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource);
    }
}
