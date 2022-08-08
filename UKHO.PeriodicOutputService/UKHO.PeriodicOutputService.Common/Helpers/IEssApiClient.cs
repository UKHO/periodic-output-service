namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IEssApiClient
    {
        Task<HttpResponseMessage> PostProductIdentifiersDataAsync(string uri, List<string> productIdentifierModel, string accessToken);
    }
}
