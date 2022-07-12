namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IExchangeSetApiClient
    {
        Task<HttpResponseMessage> PostProductIdentifiersDataAsync(string baseUrl, List<string> productIdentifierModel, string accessToken);
    }
}
