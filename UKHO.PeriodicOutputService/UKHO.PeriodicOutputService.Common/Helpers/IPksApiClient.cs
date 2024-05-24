namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IPksApiClient
    {
        Task<HttpResponseMessage> PostPksDataAsync(string uri, string requestBody, string accessToken, string? correlationId = null);
    }
}
