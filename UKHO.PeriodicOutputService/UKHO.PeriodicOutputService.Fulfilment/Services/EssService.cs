using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
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

        public async Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers)
        {
            _logger.LogInformation(EventIds.PostProductIdentifiersToEssStarted.ToEventId(), "Request to post {ProductIdentifiersCount} productidentifiers to ESS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", productIdentifiers.Count.ToString(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_essApiConfiguration.Value.BaseUrl}/productData/productIdentifiers";
            string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId);


            HttpResponseMessage httpResponse = await _essApiClient.PostProductIdentifiersDataAsync(uri, productIdentifiers, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.PostProductIdentifiersToEssCompleted.ToEventId(), "Request to post productidentifiers to ESS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);
            }
            else
            {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    _logger.LogError(EventIds.ExchangeSetNotModified.ToEventId(), "Exchange set not modified | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                }
                else
                {
                    _logger.LogError(EventIds.PostProductIdentifiersToEssFailed.ToEventId(), "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                }

                throw new FulfilmentException(EventIds.PostProductIdentifiersToEssFailed.ToEventId());
            }
        }

        public async Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime)
        {
            _logger.LogInformation(EventIds.GetProductDataSinceDateTimeStarted.ToEventId(), "ESS request to create exchange set for data since {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_essApiConfiguration.Value.BaseUrl}/productData?sinceDateTime=" + sinceDateTime;
            string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId);

            HttpResponseMessage httpResponse = await _essApiClient.GetProductDataSinceDateTime(uri, sinceDateTime, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.GetProductDataSinceDateTimeCompleted.ToEventId(), "ESS request to create exhchange set for data since {SinceDateTime} completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                ExchangeSetResponseModel exchangeSetResponseModel = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);
                exchangeSetResponseModel.ResponseDateTime = httpResponse.Headers.Date.Value.UtcDateTime;

                return exchangeSetResponseModel;
            }
            else
            {
                _logger.LogError(EventIds.GetProductDataSinceDateTimeFailed.ToEventId(), "Failed to create exchange set for data since {SinceDateTime} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.GetProductDataSinceDateTimeFailed.ToEventId());
            }
        }
    }
}
