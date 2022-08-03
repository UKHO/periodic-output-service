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

            //string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5MzQ1MDIzLCJuYmYiOjE2NTkzNDUwMjMsImV4cCI6MTY1OTM0ODk5MiwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQUJudkdObm1EMEZyUjNIcWdCSEdPWXhIWnA5MGIyREhmNzZjcWpCdmhDbmZSdjBnUVZ0YnVldDBoWDByWmpXaTBQTXFtR05XL2twUnk3V2w5dlM3My9USHFXc0pPVXhHaGJDYk9tckRQR0ZjPSIsImFtciI6WyJwd2QiXSwiYXBwaWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTcuMjI0IiwibmFtZSI6IlN1bW9kIEVkYXRoYXJhIiwib2lkIjoiMDY5NjYxMTgtOTkyMC00N2VmLTk2MDQtMTRiYzkzY2M3ZGYwIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBvdkdwb0NxV2FSSms1cDVhUDk1MW5ZQ0FDay4iLCJyb2xlcyI6WyJFeGNoYW5nZVNldFNlcnZpY2VVc2VyIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IkRTRnotRzE3el9uWF82dGRwSUMtYVhuMlR1WXdsQlVpUEM3N1RieFpKRVkiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsInV0aSI6ImhqZEg4Yy1ncEVHcHFCeGF3TTByQUEiLCJ2ZXIiOiIxLjAifQ.eAWzeb42xmAfTW81YwTTjLOWIqJ7vfa6PJTU1Qff4i4gpnuChrBGnWpeJUVhLmR0tP5uHq9jbuAyX8dFUNDtInj1Ll3wpf5zFSpqzYQbBzU2pN8Tc5gIgP_EafsGSCaVMp0Q-r3Dsm6nV_Lu-ZEor_C1Uc5QjyqR9JZvIaCoxoKF2eHGqwLk6voSzEzHvjthbyn4NqefjqQ4sMgtPLixsr7anmxY7JZgnaVOiJAQR-l8wqFn1BOjYwobKprRAH9o0PCEcsOflGE97Sl3c1WiS-n9pN1knJnCRBzXnCW4todFAm1Rwq5K-GDbv-eoM0Hci6bgpP3v8upwvhz49gIZ0g";
            string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_exchangeSetApiConfiguration.Value.EssClientId);

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
