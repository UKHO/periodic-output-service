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
                _logger.LogError(EventIds.PostProductIdentifiersToEssFailed.ToEventId(), "Failed to post productidentifiers to ESS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.PostProductIdentifiersToEssFailed.ToEventId());
            }
        }

        public async Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime)
        {
            _logger.LogInformation(EventIds.GetProductDataSinceDateTimeStarted.ToEventId(), "ESS request to create exchange set for data since {SinceDateTime} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_essApiConfiguration.Value.BaseUrl}/productData?sinceDateTime=" + sinceDateTime;

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwNzE3MDU2LCJuYmYiOjE2NjA3MTcwNTYsImV4cCI6MTY2MDcyMTczMiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUZPVFN3aHk1bmxObzI0Y2ZlQ0dCTzlSVEVRYkd6bUJDMGlYSVJvZnhxYzdMTXpoMFlKTlRmRVhuQmFrVmRsN1o1S1E4Y21RU21FeEdpTFpXT2V6K05EWnUyeTE3RU14dTBnTDJBaTVjVjBXNVVhc1R3dDdEdUV0N2ZPdjEzSGZWZzhGTEZqVFZ0RHJ2eHlib1plVy9zM3kvdGZXbVN2MGFoYWMrdlZJV0hGST0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExNSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBvdkdwb0NxV2FSSms1cDVhUDk1MW5ZQ0FCOC4iLCJyb2xlcyI6WyJFeGNoYW5nZVNldFNlcnZpY2VVc2VyIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IkljLURaS2FBTjZWX0liQmVZNXA3OFFrWVdOdjdsTlFFZnNzVk9TaU4wcVUiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6IlZpc2hhbDE0NTgzQG1hc3Rlay5jb20iLCJ1dGkiOiJFeTZmaWVnSHJVT09HU3NrUFA5cEFBIiwidmVyIjoiMS4wIn0.Io5RUnA1BEOCLNQZkFs5O1Cgvno8yK_0MPO8DrRPItA63Ox4U1FcAsSoXLki0jzv4BB8adDgWqeNuU9Dl3PNTnubTn99PX8faU1hX6RIo2inVKmTcLQHSmF_Tbdeo6almkFl7674XQCfTn_Ht_zOZ7rxHuG3R__c8d6wE00TRG5B41RjgbjZ0JdMBcEwq5lQdGKSoingUjOr4YSj1F9_p3s0DBVjPbfHGIZ0PD5a-McfuSfk8PzYEd4e-OHFT0NFsj03mkWVatepDioblVq-T_gPAY7_jAfndVUEA2kODEwzeyLYUanc1iRqJwc3_TPdt1RKXYlF-HlQKJLgOZZDgw";
            //string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId);

            HttpResponseMessage httpResponse = await _essApiClient.GetProductDataSinceDateTime(uri, sinceDateTime, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(EventIds.GetProductDataSinceDateTimeCompleted.ToEventId(), "ESS request to create exhchange set for data since {SinceDateTime} completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return JsonConvert.DeserializeObject<ExchangeSetResponseModel>(bodyJson);
            }
            else
            {
                _logger.LogError(EventIds.GetProductDataSinceDateTimeFailed.ToEventId(), "Failed to create exchange set for data since {SinceDateTime} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", sinceDateTime, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.GetProductDataSinceDateTimeFailed.ToEventId());
            }
        }
    }
}
