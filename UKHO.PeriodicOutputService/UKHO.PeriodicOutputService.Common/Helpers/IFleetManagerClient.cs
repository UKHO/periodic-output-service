namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFleetManagerClient
    {
        Task<HttpResponseMessage> GetJwtAuthUnpToken(HttpMethod method, string baseUrl, string base64Credentials, string subscriptionKey);        
        Task<HttpResponseMessage> GetCatalogue(HttpMethod method, string baseUrl, string accessToken, string subscriptionKey);
    }
}
