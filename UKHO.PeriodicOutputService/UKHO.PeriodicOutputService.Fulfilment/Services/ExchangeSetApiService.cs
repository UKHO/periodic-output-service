using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class ExchangeSetApiService : IExchangeSetApiService
    {
        private readonly IOptions<ExchangeSetApiConfiguration> _exchangeSetApiConfiguration;
        private readonly IExchangeSetApiClient _exchangeSetApiClient;
        private readonly IAuthTokenProvider _authTokenProvider;
        private readonly ILogger<ExchangeSetApiService> _logger;

        public ExchangeSetApiService(ILogger<ExchangeSetApiService>? logger,
                                     IOptions<ExchangeSetApiConfiguration>? exchangeSetApiConfiguration,
                                     IExchangeSetApiClient? exchangeSetApiClient, IAuthTokenProvider? authTokenProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetApiConfiguration = exchangeSetApiConfiguration ?? throw new ArgumentNullException(nameof(exchangeSetApiConfiguration));
            _exchangeSetApiClient = exchangeSetApiClient ?? throw new ArgumentNullException(nameof(exchangeSetApiClient));
            _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
        }

        public async Task<ExchangeSetGetBatchResponse> GetProductIdentifiersData(List<string> productIdentifiers)
        {
            _logger.LogInformation(EventIds.AccessTokenForESSEndpointStarted.ToEventId(), "Getting access token to call ESS endpoint started");

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU3MTA1NzgxLCJuYmYiOjE2NTcxMDU3ODEsImV4cCI6MTY1NzExMTE5OSwiYWNyIjoiMSIsImFpbyI6IkFaUUFhLzhUQUFBQW5VRGNKbExqbktzNzVrajVTMVppcVZ1ZCtwNFlKSlkxL0VWSHg3Ym9LNWRlcC9CQWJRZ2FFWVFSaU5BSXZvdTJIYy9qRHEwcXRCZ2FvY1ozYXl4KzVvQzgvUHIzOVZaNWxQRVVEd2w3UGxyaUJjTmRwRU9LMjhQQVFXUWo2UysxTVl5RzlrVWhmRUEvOGNNWFo1cm5JOGpmQzJEWXdEWWhXc29uaGN6K2w5c1hmYVd5SlVTTTQvZmZ5cjBlSDNucyIsImFtciI6WyJwd2QiLCJyc2EiLCJtZmEiXSwiYXBwaWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTIuMjA0IiwibmFtZSI6IlN1bW9kIEVkYXRoYXJhIiwib2lkIjoiMDY5NjYxMTgtOTkyMC00N2VmLTk2MDQtMTRiYzkzY2M3ZGYwIiwicHdkX2V4cCI6Ijk0MTQ4NSIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgiLCJyaCI6IjAuQVZNQVNNbzBrVDFtQlVxV2lqR2tMd3J0UG92R3BvQ3FXYVJKazVwNWFQOTUxbllDQUNrLiIsInJvbGVzIjpbIkV4Y2hhbmdlU2V0U2VydmljZVVzZXIiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiRFNGei1HMTd6X25YXzZ0ZHBJQy1hWG4yVHVZd2xCVWlQQzc3VGJ4WkpFWSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoic3Vtb2QxMzk0M0BtYXN0ZWsuY29tIiwidXRpIjoiZFlRV0pRZXdaMHlPWk94LXBucnRBQSIsInZlciI6IjEuMCJ9.PR5c_qKZdXnmkYGzToRaH0haK-f25qtN5u8z3ybXQxcCZRBquVOfIcC0d92NTRhr3BHkO4SjAA5OiYyuas9xVyePGBCdOgQ9bDV4oNN4PwaLJGnQCP7YIOJU7RatCw1TTtOUCoYUvhWXhWujEPS4KiqfQzblaoAY60WSLgTSUnzzTyK8YCZ0jkeFAn30yfzWe2AJUFIB-N5nEv0cdP6tpclwToGHmmXjqMFEr5WWM8aM_TldhRXCTNmvCpofXxtHrPFkdCM4KtbyrnLuu1j2x6gsPtT64Qtabi5bVrp3uWMW2CQLJqoNmDcmrqeTbPSE05wLE20lZmvM1uLxsL_xkg";

                //await _authTokenProvider.GetManagedIdentityAuthAsync(_exchangeSetApiConfiguration.Value.EssClientId);

            _logger.LogInformation(EventIds.AccessTokenForESSEndpointCompleted.ToEventId(), "Getting access token  to call ESS endpoint completed");

            ExchangeSetGetBatchResponse exchangeSetGetBatchResponse = new();

            _logger.LogInformation(EventIds.ExchangeSetRequestStarted.ToEventId(), "Request to get the exchange set details started");

            HttpResponseMessage httpResponse = await _exchangeSetApiClient.GetProductIdentifiersDataAsync(_exchangeSetApiConfiguration.Value.BaseUrl, productIdentifiers, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                exchangeSetGetBatchResponse = JsonConvert.DeserializeObject<ExchangeSetGetBatchResponse>(bodyJson);
            }
            else
            {
                _logger.LogError(EventIds.ExceptionInExchangeSetRequest.ToEventId(), "Failed getting exchange set details");

            }

            _logger.LogInformation(EventIds.ExchangeSetRequestCompleted.ToEventId(), "Request to get the exchange set details completed");

            return exchangeSetGetBatchResponse;
        }
    }
}
