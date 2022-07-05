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

        public ExchangeSetApiService(ILogger<ExchangeSetApiService> logger,
                                     IOptions<ExchangeSetApiConfiguration> exchangeSetApiConfiguration,
                                     IExchangeSetApiClient exchangeSetApiClient, IAuthTokenProvider authTokenProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetApiConfiguration = exchangeSetApiConfiguration ?? throw new ArgumentNullException(nameof(exchangeSetApiConfiguration));
            _exchangeSetApiClient = exchangeSetApiClient ?? throw new ArgumentNullException(nameof(exchangeSetApiClient));
            _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
        }

        public async Task<ExchangeSetGetBatchResponse> GetProductIdentifiersData(List<string> productIdentifiers)
        {
            _logger.LogInformation(EventIds.AccessTokenForESSEndpointStarted.ToEventId(), "Getting access token to call ESS endpoint started");

            //string accessToken = await _authTokenProvider.GetManagedIdentityAuthAsync(_exchangeSetApiConfiguration.Value.EssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU3MDM0NDg3LCJuYmYiOjE2NTcwMzQ0ODcsImV4cCI6MTY1NzAzODgxNywiYWNyIjoiMSIsImFpbyI6IkFaUUFhLzhUQUFBQXNYOWdkQnd6WUxaQWJKUkxjTDJRZE1OUm9waEhZUjdzWVRsNURYUnJnL1dTREVYeFBDdGM3NXc0WWJ2b3ZGUUMrS2MvZUIvRnEzUFdQMWhMbzM0ZjBGUWdlRElTRzlVRVQ2MEE3WnVtM1dqOUgrVmIrbDdRWHZwRTRBYzdOeVpKNzJoOHMybHJpVEgyNENMdU5uekVPcFNIUG1NK3hOaXlHR1o0U1FzWmsvbWNGWXM4NFMzcmV5VmJvbHFMWjRKNSIsImFtciI6WyJwd2QiLCJyc2EiLCJtZmEiXSwiYXBwaWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTIuNzYiLCJuYW1lIjoiU3Vtb2QgRWRhdGhhcmEiLCJvaWQiOiIwNjk2NjExOC05OTIwLTQ3ZWYtOTYwNC0xNGJjOTNjYzdkZjAiLCJwd2RfZXhwIjoiMTAxMjc4MCIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgiLCJyaCI6IjAuQVZNQVNNbzBrVDFtQlVxV2lqR2tMd3J0UG92R3BvQ3FXYVJKazVwNWFQOTUxbllDQUNrLiIsInJvbGVzIjpbIkV4Y2hhbmdlU2V0U2VydmljZVVzZXIiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiRFNGei1HMTd6X25YXzZ0ZHBJQy1hWG4yVHVZd2xCVWlQQzc3VGJ4WkpFWSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoic3Vtb2QxMzk0M0BtYXN0ZWsuY29tIiwidXRpIjoiTzZ4ZzJlMVM1MGlsUmNCWEkzM1dBQSIsInZlciI6IjEuMCJ9.OBljgDmAfpRKdvDeDu4zLKlwHPguFa1lEs2xkfvlGRgU28TJs8HO92VtMaF8_7vGNu6DFpQqQc3bsFlD_G4Bb-yYLcPmMENcvulpvIRLjkMrOErqLMmMgJ7y8yrouWO4HMQAxKEfhnGbDOENoZPrp8IIwbZRszEtJAHxwr8cHbarEmN9DIUf4yvVjpdNQ98Pr4bB75BVteFTqpmbNS-XLggMMJYVdJrJ3Tp93oRK1DH4HbnnieUjCIlSyxhE6qjQoxfa9sLxQ3RcUhvL4pFilN7c9jQzzGyPitO6p3XzX_ldQbO-UQoCxVtEtuogSF9YOlZCwa1YwutoOXKfrErHIA";

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
