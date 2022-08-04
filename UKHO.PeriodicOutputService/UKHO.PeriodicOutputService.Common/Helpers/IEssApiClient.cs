namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IEssApiClient
    {
        Task<HttpResponseMessage> PostProductIdentifiersDataAsync(string baseUrl, List<string> productIdentifierModel, string accessToken);
    }
}
