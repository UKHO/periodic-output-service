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

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODA5LCJuYmYiOjE2NTk1MzE4MDksImV4cCI6MTY1OTUzNzE4NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUtLMzZScFF3SHZxQ1pmbFB6L0t4VUlYSmh1L2JLQjFweHJRbE5NbFhHWW93T0FYMlpDSDJyWWtMRmtQMVJEaU9PcE5EUnQ5QzVvUWh0eUk2MFV1eEpPWlhnZ0xkUE1MclhZSHAxZFZIT1VUOVBNbnQzMWl2MGFWaGxVd29ua3VxZ3J6eHhiUndwNTFwS3JwcXk3TnpHQWZJM3BjSGkrZFBvaC9CT0g4YTR5OD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQb3ZHcG9DcVdhUkprNXA1YVA5NTFuWUNBSXMuIiwicm9sZXMiOlsiRXhjaGFuZ2VTZXRTZXJ2aWNlVXNlciJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiJFaEUxRUVqeDhPVXJabTlyeGFHQ2NSZ2FQUS05eWpqbVZoR1lOaVdHMmk4IiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsInV0aSI6ImtyLU1QSDlwTDB5dlZTU0JncnhWQUEiLCJ2ZXIiOiIxLjAifQ.i8x0KJeX70pRjhvk6IiDmWAqBIYCtdZBiufsnGW-LNT2dfaAHQhC6SXZ6RhvlIRQ4b-PgIEIlgCy2o1yasQo3Mu5t8TeYml4c5IScFQAB95sVtudsieo9PAV-jN4eOvg3jJd4nIU7OqDHnjn30keesA58A81p7eUHwFXea0LGcYtotYnCPpyQhHwknFmgEFZnO2MiK7l0CFN9ky4AZsKXULS2X08gn7zMaIH7wtVl9SH2bYcPgPJ6gFmVsZuHU7IHCC1rAhD8lReYnxPJVGX75ngOh9K5sUtpzrW8eB68Fj6dgFxjneS21sSbpJCNJUUKpZrqo4g4l7Ru6got5qn1w";
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
