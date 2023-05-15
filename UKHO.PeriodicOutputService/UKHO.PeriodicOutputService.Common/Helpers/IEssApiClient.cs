using UKHO.PeriodicOutputService.Common.Models.Ess;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IEssApiClient
    {
        Task<HttpResponseMessage> PostProductIdentifiersDataAsync(string uri, List<string> productIdentifierModel, string accessToken);
        Task<HttpResponseMessage> GetProductDataSinceDateTime(string uri, string sinceDateTime, string accessToken);

        Task<HttpResponseMessage> GetProductDataProductVersion(string uri, List<ProductVersion> productVersions, string accessToken);
    }
}
