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
            _logger.LogInformation(EventIds.GetAccessTokenForESSEndPointStarted.ToEventId(), "Getting access token to call ESS endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwMDQ2MjEyLCJuYmYiOjE2NjAwNDYyMTIsImV4cCI6MTY2MDA1MDkxNCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUJYS1NLQ2FmYVJvbXRXb2M0V1pzRUxPTUUzUUplcytqMkRyWUFkcHhiU1UrRjM2K0ZIZituTmJDVy9JTTdGZjhpaFVpc0ZLWVllN3laRWppT3ZhTHo3R0VhaHhic0ppUHZVbVI5NnBTZDRGbjkraHlWTTRlZlVKZVpNRG1scGU0MER5azNhUWh5UTN1YXUxWHpFWnFyRjVRYTFqWmNvbEhHOFdNeUdLMXF5WT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjEwOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicHdkX2V4cCI6IjIzMzE5NSIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgiLCJyaCI6IjAuQVFJQVNNbzBrVDFtQlVxV2lqR2tMd3J0UG92R3BvQ3FXYVJKazVwNWFQOTUxbllDQUI4LiIsInJvbGVzIjpbIkV4Y2hhbmdlU2V0U2VydmljZVVzZXIiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiSWMtRFpLYUFONlZfSWJCZVk1cDc4UWtZV052N2xOUUVmc3NWT1NpTjBxVSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiVmlzaGFsMTQ1ODNAbWFzdGVrLmNvbSIsInV0aSI6ImVOSUxic2pvWkVtbmFIb051TVFVQUEiLCJ2ZXIiOiIxLjAifQ.o-qqwxKIt2ENm_BjIRjlKTRjqxre-BkgSKRa8c9K0okFmJoR6POMJkncnEpBJ_qRsYEfj2TddLJfDJVE1h-965JZkPf_8Thf9CcDQFNLhczsQtHETZxRoLiC2Px25M5WBOmX21ER63h0jkVTTM1zr-nIiEFTTEREUN4tJpbyXpQwQOjiwU4IhJhyxe-1NOvg8_5rnkfj35xLr5noPbG-ssOvDmTxVdFiP9p1OSawg2VEzpCeuh_UFeq42MH9XZEOLYmP7IrGVW0G2B2LuGyct3934Lc2dATmusJWPCbsfFQoRba1vCCdSOSGNULXtNOCQ9uwunTymXUCxum1W9TTYA";
            //string accessToken = await _authEssTokenProvider.GetManagedIdentityAuthAsync(_essApiConfiguration.Value.EssClientId);

            _logger.LogInformation(EventIds.GetAccessTokenForESSEndPointCompleted.ToEventId(), "Getting access token to call ESS endpoint completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            ExchangeSetResponseModel? exchangeSetGetBatchResponse = new();

            _logger.LogInformation(EventIds.ExchangeSetPostProductIdentifiersRequestStarted.ToEventId(), "Request to post productidentifiers to ESS started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            HttpResponseMessage httpResponse = await _essApiClient.PostProductIdentifiersDataAsync(_essApiConfiguration.Value.BaseUrl, productIdentifiers, accessToken);

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
