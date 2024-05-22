using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public class EssService : IEssService
    {
        private readonly IOptions<EssApiConfiguration> _essApiConfiguration;
        private readonly IEssApiClient _essApiClient;
        private readonly IAuthEssTokenProvider _authEssTokenProvider;
        private readonly ILogger<EssService> _logger;

        public EssService(ILogger<EssService> logger,
                                     IOptions<EssApiConfiguration> essApiConfiguration,
                                     IEssApiClient essApiClient,
                                     IAuthEssTokenProvider authEssTokenProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _essApiConfiguration = essApiConfiguration ?? throw new ArgumentNullException(nameof(essApiConfiguration));
            _essApiClient = essApiClient ?? throw new ArgumentNullException(nameof(essApiClient));
            _authEssTokenProvider = authEssTokenProvider ?? throw new ArgumentNullException(nameof(authEssTokenProvider));
        }

        public async Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers, string? exchangeSetStandard = null, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.PostProductIdentifiersToEssStarted.ToEventId(), "Request to post {ProductIdentifiersCount} productidentifiers to ESS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", productIdentifiers.Count.ToString(), DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = GetProductIdentifierUri(_essApiConfiguration.Value.BaseUrl, exchangeSetStandard);
            string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId, CommonHelper.GetCorrelationId(correlationId));

            HttpResponseMessage httpResponse = await _essApiClient.PostProductIdentifiersDataAsync(uri, productIdentifiers, accessToken, CommonHelper.GetCorrelationId(correlationId));

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.PostProductIdentifiersToEssCompleted.ToEventId(), "Request to post productidentifiers to ESS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                return JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);
            }
            else
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    _logger.LogError(EventIds.ExchangeSetNotModified.ToEventId(), "Exchange set not modified | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                }
                else
                {
                    _logger.LogError(EventIds.PostProductIdentifiersToEssFailed.ToEventId(), "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                }

                throw new FulfilmentException(EventIds.PostProductIdentifiersToEssFailed.ToEventId());
            }
        }

        public async Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime, string? exchangeSetStandard = null, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.GetProductDataSinceDateTimeStarted.ToEventId(), "ESS request to create exchange set for data since {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = GetSinceDateTimeUri(_essApiConfiguration.Value.BaseUrl, sinceDateTime, exchangeSetStandard);
            string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId, CommonHelper.GetCorrelationId(correlationId));

            HttpResponseMessage httpResponse = await _essApiClient.GetProductDataSinceDateTime(uri, sinceDateTime, accessToken, CommonHelper.GetCorrelationId(correlationId));

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.GetProductDataSinceDateTimeCompleted.ToEventId(), "ESS request to create exhchange set for data since {SinceDateTime} completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                ExchangeSetResponseModel? exchangeSetResponseModel = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);
                exchangeSetResponseModel!.ResponseDateTime = httpResponse!.Headers!.Date!.Value.UtcDateTime;

                return exchangeSetResponseModel;
            }
            else
            {
                _logger.LogError(EventIds.GetProductDataSinceDateTimeFailed.ToEventId(), "Failed to create exchange set for data since {SinceDateTime} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                throw new FulfilmentException(EventIds.GetProductDataSinceDateTimeFailed.ToEventId());
            }
        }

        public async Task<ExchangeSetResponseModel?> GetProductDataProductVersions(ProductVersionsRequest productVersionsRequest, string? exchangeSetStandard = null, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.GetProductDataProductVersionStarted.ToEventId(), "ESS request to create exchange set for product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = GetProductVersionUri(_essApiConfiguration.Value.BaseUrl, exchangeSetStandard);
            string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId, CommonHelper.GetCorrelationId(correlationId));

            HttpResponseMessage httpResponse = await _essApiClient.GetProductDataProductVersion(uri, productVersionsRequest.ProductVersions, accessToken, CommonHelper.GetCorrelationId(correlationId));

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.GetProductDataProductVersionCompleted.ToEventId(), "ESS request to create exchange set for product version completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                ExchangeSetResponseModel? exchangeSetResponseModel = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);

                return exchangeSetResponseModel;
            }
            else
            {
                _logger.LogError(EventIds.GetProductDataProductVersionFailed.ToEventId(), "Failed to create exchange set for product version | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                throw new FulfilmentException(EventIds.GetProductDataProductVersionFailed.ToEventId());
            }
        }

        [ExcludeFromCodeCoverage]
        private static string GetProductIdentifierUri(string url, string exchangeSetStandard) =>
            !string.IsNullOrEmpty(exchangeSetStandard) ? $"{url}/productData/productIdentifiers?exchangeSetStandard={exchangeSetStandard}" : $"{url}/productData/productIdentifiers";

        [ExcludeFromCodeCoverage]
        private static string GetSinceDateTimeUri(string url, string sinceDateTime, string exchangeSetStandard) =>
            !string.IsNullOrEmpty(exchangeSetStandard) ? $"{url}/productData?sinceDateTime={sinceDateTime}&exchangeSetStandard={exchangeSetStandard}" : $"{url}/productData?sinceDateTime={sinceDateTime}";

        [ExcludeFromCodeCoverage]
        private static string GetProductVersionUri(string url, string exchangeSetStandard) =>
            !string.IsNullOrEmpty(exchangeSetStandard) ? $"{url}/productData/productVersions?exchangeSetStandard={exchangeSetStandard}" : $"{url}/productData/productVersions";
    }
}
