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
            //string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NTkxLCJuYmYiOjE2NjA4MTU1OTEsImV4cCI6MTY2MDgyMDIyMCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUpmMzllZnF1a2RNYzBhQkpHdmxwaFpSZmQ0K29YMFpBamtlRkZ3TTBqU2lUTWZ6U3k0aEpwM0VBR0NwSW5DMEhUeEcyUE5tcTJWZmRpbUhFVGtGME9NUHEvaXF6OUloaWN3elBESXl0THQyL2pqSFlQUWRCb1VTaUY0R0lZcVF5OGU1YnZSYkJjUEdkbXRkRHltcklpSzZmUGFRei9CRGpLZkUvTmNNV0l2bz0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBvdkdwb0NxV2FSSms1cDVhUDk1MW5ZQ0FCOC4iLCJyb2xlcyI6WyJFeGNoYW5nZVNldFNlcnZpY2VVc2VyIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IkljLURaS2FBTjZWX0liQmVZNXA3OFFrWVdOdjdsTlFFZnNzVk9TaU4wcVUiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6IlZpc2hhbDE0NTgzQG1hc3Rlay5jb20iLCJ1dGkiOiJaYWItYTRDZFRVaWJoWEVUaGJkakFBIiwidmVyIjoiMS4wIn0.J3ThenS4fFhGHajM2f2mTGTT-YCgizabrE6pg8p5UPLvt5e6yoJDnc4FrqHK8DEweeepfKeEyjrpwZKYYYy_6FZGlWwFh-R_Z1wjhpp4uukgTAADxg_GUo0oBJTFUb3UJCDkUQ3Onam_bPjTUZ5dhzwhASGDWSLikozK32DEAGA_r1gpVCutv-ygrUvUAFgsbx21W0YHMG_I5QiBQ0Yyxy4MUdOFgyaHdq6HVVAvZZ4xgb0t-qsoU0i-Kh2QTK2hW-7eYO4fjuIlAyutZAQW5g3-UiQgQvC_nwj5FyTuSLfcbEy_ZJgZtn5luH6hqFHx2Qc4prS5MfG6BqtZqSbBsg";

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
