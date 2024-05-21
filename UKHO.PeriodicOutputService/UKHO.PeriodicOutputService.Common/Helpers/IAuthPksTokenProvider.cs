namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAuthPksTokenProvider
    {
        public Task<string> GetManagedIdentityAuthAsync(string resource, string? correlationId = null);
    }
}
