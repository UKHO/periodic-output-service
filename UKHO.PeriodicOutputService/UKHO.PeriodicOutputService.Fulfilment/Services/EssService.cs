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
        private readonly IOptions<ExchangeSetApiConfiguration> _exchangeSetApiConfiguration;
        private readonly IEssApiClient _exchangeSetApiClient;
        private readonly IAuthEssTokenProvider _authEssTokenProvider;
        private readonly ILogger<EssService> _logger;

        public EssService(ILogger<EssService> logger,
                                     IOptions<ExchangeSetApiConfiguration> exchangeSetApiConfiguration,
                                     IEssApiClient exchangeSetApiClient,
                                     IAuthEssTokenProvider authEssTokenProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetApiConfiguration = exchangeSetApiConfiguration ?? throw new ArgumentNullException(nameof(exchangeSetApiConfiguration));
            _exchangeSetApiClient = exchangeSetApiClient ?? throw new ArgumentNullException(nameof(exchangeSetApiClient));
            _authEssTokenProvider = authEssTokenProvider ?? throw new ArgumentNullException(nameof(authEssTokenProvider));
        }

        public async Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers)
        {
            _logger.LogInformation(EventIds.GetAccessTokenForESSEndPointStarted.ToEventId(), "Request to get access token to call ESS endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4ODQ0ODUzLCJuYmYiOjE2NTg4NDQ4NTMsImV4cCI6MTY1ODg0OTI2NSwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQTJ3MVJITFcrVXNFSkhndjEwQTZJZlZLUGxkZndtNG5HbWRtMkltMzhsck5oeE4rSnk5QTlCcDFZOFdWOVNtbWdBYkRQbTY2cERQbTJyZUNyUkRXOGpDcWFwbC9oSndoQmVrQWpIS1VlUWdzPSIsImFtciI6WyJwd2QiXSwiYXBwaWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTcuNzAiLCJuYW1lIjoiU3Vtb2QgRWRhdGhhcmEiLCJvaWQiOiIwNjk2NjExOC05OTIwLTQ3ZWYtOTYwNC0xNGJjOTNjYzdkZjAiLCJyaCI6IjAuQVZNQVNNbzBrVDFtQlVxV2lqR2tMd3J0UG92R3BvQ3FXYVJKazVwNWFQOTUxbllDQUNrLiIsInJvbGVzIjpbIkV4Y2hhbmdlU2V0U2VydmljZVVzZXIiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiRFNGei1HMTd6X25YXzZ0ZHBJQy1hWG4yVHVZd2xCVWlQQzc3VGJ4WkpFWSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoic3Vtb2QxMzk0M0BtYXN0ZWsuY29tIiwidXRpIjoiT0N5T2tYN1Zqa3FsOWV2TzVtQjVBQSIsInZlciI6IjEuMCJ9.cpnZj82YQpvH4NU-HuO1RBZdA4gXqS2KNpFMu6hcy4ydY_m_GX7Bbx88IXApEkyjc4YcTLqgYygdHvkgDWJGB_wRPksz5iWXqb13wvuu8--GO1vgOjC7Hb6YOR1dzZU7hAOjyK48YvVMEGnqKD8vcuk8orCT6QTdTPvun8RcoLNJx-YpzBJRWDJKNmjbCbiHMQTiH3LiW1t_u1-0p6wcvmpzlz6SV_8twhfDWJUkHuIu3fj-7jhi0ym1Siz5Z_zer8c3RSB8VXEE_SqsIdHJuYqVKHoZT9bb9Hdfk1mw2UT8yAUnFn9-nbdkFiS-XR61u2JZgLcFZDWOfNRLiGIjSw";
            //string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_exchangeSetApiConfiguration.Value.EssClientId);

            _logger.LogInformation(EventIds.GetAccessTokenForESSEndPointCompleted.ToEventId(), "Request to get access token to call ESS endpoint completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            ExchangeSetResponseModel? exchangeSetGetBatchResponse = new();

            _logger.LogInformation(EventIds.ExchangeSetPostProductIdentifiersRequestStarted.ToEventId(), "Request to post productidentifiers to ESS started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            HttpResponseMessage httpResponse = await _exchangeSetApiClient.PostProductIdentifiersDataAsync(_exchangeSetApiConfiguration.Value.BaseUrl, productIdentifiers, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();

                exchangeSetGetBatchResponse = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);

                _logger.LogInformation(EventIds.ExchangeSetPostProductIdentifiersRequestCompleted.ToEventId(), "Request to post productidentifiers to ESS completed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
            }
            else
            {
                _logger.LogError(EventIds.ExchangeSetPostProductIdentifiersFailed.ToEventId(), "Failed to post productidentifiers to ESS at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
            }
            return exchangeSetGetBatchResponse;
        }
    }
}
