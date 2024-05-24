namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAuthFssTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource, string? correlationId = null);
    }
}
