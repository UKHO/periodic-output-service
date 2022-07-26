using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
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

            //string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4ODI0NTczLCJuYmYiOjE2NTg4MjQ1NzMsImV4cCI6MTY1ODgyOTAwNSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUpmMzllZnF1a2RNYzBhQkpHdmxwaGRJbmhGZHhNSmJJUmc0Q1lUQlhUdnNwZERYUGpsWXhvNVBHZ3BmTjMzd3dxb3dOVUorbFRTVVNtWGlKVmEyNFM5bW5OejFPaWpuSFZtK3RXN21wZFVES2cyZTBrR2dVV1FqN3RTQkkzRzM3YXFJV21aWDZneW5VRFRnMGFRdjY0MnJ2QXpFNGdCN1dnZFowaUZyUWx3ND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNjcuMTc2LjE4OCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBvdkdwb0NxV2FSSms1cDVhUDk1MW5ZQ0FCOC4iLCJyb2xlcyI6WyJFeGNoYW5nZVNldFNlcnZpY2VVc2VyIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IkljLURaS2FBTjZWX0liQmVZNXA3OFFrWVdOdjdsTlFFZnNzVk9TaU4wcVUiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6IlZpc2hhbDE0NTgzQG1hc3Rlay5jb20iLCJ1dGkiOiIzNmFIYTl1WDUwLUxuMFVSaGdrT0FBIiwidmVyIjoiMS4wIn0.qetHEx0lp8mXuZuTk8Mcxqdsu8rMb2JyLnaPha9hGIv6dhqKS2dIY15ZwQWkLh_830CcXyAr2L2cEEEWPfFwVRiYd-cjNMhgA-c5vrkGtfE60UJin5YTz5luFAwMuKkdSpJfaTyiZOcePYdcuHwEQzzKCcB_JPzEchl3LkbWKYMtRFQcDTwU4TmtjnIUF7mz3ykU42GDh7jMlLRC4mvXAFpjqwaN9n8yf_vOJRt98OxS0YMMswz2etD3TxylPpuJ_B329Jlyog3Wi7MgqNjsNZhes-VOBkbAxO89aC-F-uL6kTJcS1C_UpUEXAa1O7PTwSHK5EJZf2lqyrWl0JHU6w";
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
