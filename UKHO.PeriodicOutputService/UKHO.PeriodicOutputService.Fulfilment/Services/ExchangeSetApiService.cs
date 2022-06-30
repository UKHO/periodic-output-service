using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
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
            _logger.LogInformation("Started getting access token");

            string accessToken = await _authTokenProvider.GetManagedIdentityAuthAsync(_exchangeSetApiConfiguration.Value.EssClientId);

            _logger.LogInformation("Completed getting access token", accessToken);

            ExchangeSetGetBatchResponse exchangeSetGetBatchResponse = new();

            _logger.LogInformation("Started getting product identifiers data");

            HttpResponseMessage httpResponse = await _exchangeSetApiClient.GetProductIdentifiersDataAsync(_exchangeSetApiConfiguration.Value.BaseUrl, productIdentifiers, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                exchangeSetGetBatchResponse = JsonConvert.DeserializeObject<ExchangeSetGetBatchResponse>(bodyJson);
            }
            else
            {
                _logger.LogInformation("Failed getting product identifiers data");

            }

            _logger.LogInformation("Completed getting product identifiers data");

            return exchangeSetGetBatchResponse;
        }
    }
}
