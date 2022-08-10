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

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwMTM3OTA1LCJuYmYiOjE2NjAxMzc5MDUsImV4cCI6MTY2MDE0MjcyNSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQTlBVE91cGZNYXZYM0ppWHVTM0NDbCt4NE5VTjVBUnNGbGc2Mm1HWTFaRGh3Tmhmb3Q1Q21QYWFSTWZyWjAzVGI3M1ZNTTRJN2tCWTBiSDVvRXNXRlg4WU95Ymcvd0NvUTlTdXBvc2Z5Y2dJeUtia0g4RUJTRjMwMFNpdVhVV1hGNzFVYVI2NnBHSmV2bVJvMFdRbFJicHVXWEp3MFFyeUNnUFNUclZwVXZ0VT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjEyMCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicHdkX2V4cCI6IjE0MTUwMSIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgiLCJyaCI6IjAuQVFJQVNNbzBrVDFtQlVxV2lqR2tMd3J0UG92R3BvQ3FXYVJKazVwNWFQOTUxbllDQUI4LiIsInJvbGVzIjpbIkV4Y2hhbmdlU2V0U2VydmljZVVzZXIiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiSWMtRFpLYUFONlZfSWJCZVk1cDc4UWtZV052N2xOUUVmc3NWT1NpTjBxVSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiVmlzaGFsMTQ1ODNAbWFzdGVrLmNvbSIsInV0aSI6IncyZ3dqbnZpM2tpX3ctVlZQUmhSQUEiLCJ2ZXIiOiIxLjAifQ.q0D6sQthXj5sG14yNFOagSWOV0n0a7N0cs0O3KaYV3xUBO16Ybhnc6rcb8kaMim547rhfTKYHAwJxwzSGuwGPXVMrAT6KFq7QXyd3Tabt4lWnYXvSm4d19Lp6Fixvx8R4U_J_d6vgQlTmOOeeEUegmJ_G2H_kyT4bFX9eRMQMrE2pzaLfdqIJSiLsL75-sEn1Z_iOLXVOdXlcTUHcn2rG2dpguFCR6hwGCSrwKnWKfGrBi0-dHT4IJhpWZWDU1AoCqeieqaWyErOmrKXk-bLRDppO6-Y-9RM_WRgl9lTS6H_WOyhiK3iKT2TUdzY8eubvjS3wrdpDrvMa_Ox9McG2A";
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
