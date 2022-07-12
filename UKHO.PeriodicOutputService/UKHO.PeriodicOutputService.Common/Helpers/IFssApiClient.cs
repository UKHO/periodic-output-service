namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFssApiClient
    {
        public Task<HttpResponseMessage> GetBatchStatusAsync(string uri, string accessToken);
    }
}
