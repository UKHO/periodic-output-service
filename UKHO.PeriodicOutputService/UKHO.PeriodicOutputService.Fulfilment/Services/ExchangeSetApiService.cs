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

        public ExchangeSetApiService(IOptions<ExchangeSetApiConfiguration> exchangeSetApiConfiguration,
                            IExchangeSetApiClient exchangeSetApiClient, IAuthTokenProvider authTokenProvider)
        {
            _exchangeSetApiConfiguration = exchangeSetApiConfiguration ?? throw new ArgumentNullException(nameof(exchangeSetApiConfiguration));
            _exchangeSetApiClient = exchangeSetApiClient ?? throw new ArgumentNullException(nameof(exchangeSetApiClient));
            _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
        }

        public async Task<ExchangeSetGetBatchResponse> GetProductIdentifiersData(List<string> productIdentifiers)
        {
            string accessToken = await _authTokenProvider.GetManagedIdentityAuthAsync(_exchangeSetApiConfiguration.Value.ClientId);
            ExchangeSetGetBatchResponse exchangeSetGetBatchResponse = new();

            HttpResponseMessage httpResponse = await _exchangeSetApiClient.GetProductIdentifiersDataAsync(_exchangeSetApiConfiguration.Value.BaseUrl, productIdentifiers, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                exchangeSetGetBatchResponse = JsonConvert.DeserializeObject<ExchangeSetGetBatchResponse>(bodyJson);
            }

            return exchangeSetGetBatchResponse;
        }
    }
}
