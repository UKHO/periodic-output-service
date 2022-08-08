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

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5OTczMDU2LCJuYmYiOjE2NTk5NzMwNTYsImV4cCI6MTY1OTk3ODIyOCwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQTdxY3dTUmwrb0ZUdlFCZXJOMTQrbDQ3Uk9xeGZHUjhsU2toaGNpVnJhbzF6OTljLzlDMmFNRlZ0OGxjUXhZK2ppYmFhZmZyQmZZZlZjSmVBM21tSjBUbmRWS1grK29lbVJHYVJkU0NiK29JPSIsImFtciI6WyJwd2QiXSwiYXBwaWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTcuMjM5IiwibmFtZSI6IlN1bW9kIEVkYXRoYXJhIiwib2lkIjoiMDY5NjYxMTgtOTkyMC00N2VmLTk2MDQtMTRiYzkzY2M3ZGYwIiwicmgiOiIwLkFWTUFTTW8wa1QxbUJVcVdpakdrTHdydFBvdkdwb0NxV2FSSms1cDVhUDk1MW5ZQ0FDay4iLCJyb2xlcyI6WyJFeGNoYW5nZVNldFNlcnZpY2VVc2VyIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IkRTRnotRzE3el9uWF82dGRwSUMtYVhuMlR1WXdsQlVpUEM3N1RieFpKRVkiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsInV0aSI6ImtZWVI3M0RZTGtTVkwxVlk1VHRWQUEiLCJ2ZXIiOiIxLjAifQ.A87JqOUksfq6Yk-C2UMW9pjYC13LQf7SxeBC3q65floZgBgU5ML0bhn_e866ziKIpwkJYykm1qOIiuqIHYQ8WG9tBb5KG_2jCPKofk8xykPkKP27rYOApojnjSKUZ8_sYYTuOS-8oRdN-TUjDAhjvmXrK6CgUy3bU_gXwf99DfGLxVHARz6fxs38q2Bku0MMLH7xmY27HmquDQ2jYVV6XczBFf9CcLYwy61x_mJRqA_1UmYS_mvXhv1HrppSBV2BWdo7XcmBNvZGnW4ALVfV6-OQj1s5lY3lS8b1WdSoGEz6bDhTtB_tR-Wt3TEoGkDbf4EROetocHyhbnRfWuH_vw";
            //string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId);

            HttpResponseMessage httpResponse = await _essApiClient.PostProductIdentifiersDataAsync(uri, productIdentifiers, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.PostProductIdentifiersToEssCompleted.ToEventId(), "Request to post productidentifiers to ESS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);
            }
            else
            {
                _logger.LogError(EventIds.PostProductIdentifiersToEssFailed.ToEventId(), "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.PostProductIdentifiersToEssFailed.ToEventId());
            }
        }
    }
}
