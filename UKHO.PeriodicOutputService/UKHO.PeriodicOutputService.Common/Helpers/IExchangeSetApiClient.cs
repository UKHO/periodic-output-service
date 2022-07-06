namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IExchangeSetApiClient
    {
        Task<HttpResponseMessage> GetProductIdentifiersDataAsync(string baseUrl, List<string> productIdentifierModel, string accessToken);
    }
}
