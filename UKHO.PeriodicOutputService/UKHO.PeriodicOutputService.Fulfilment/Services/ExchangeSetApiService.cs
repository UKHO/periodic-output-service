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

        private async Task<ExchangeSetApiConfiguration> GetAccessToken()
        {
            var _httpClient = new HttpClient();
            var result = "";
            //var requestURL = "https://login.microsoftonline.com/{AzureTenantId}/oauth2/v2.0/token";
            var requestURL = "https://login.microsoftonline.com/9134ca48-663d-4a05-968a-31a42f0aed3e/oauth2/v2.0/token";

            var content = new FormUrlEncodedContent(new Dictionary<string, string> {
              { "client_id", "80a6c68b-59aa-49a4-939a-7968ff79d676" },
              //{ "client_secret", "YourSecret" },
              { "grant_type", "client_credentials" },
              { "scope", "80a6c68b-59aa-49a4-939a-7968ff79d676/.default" },
            });

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(requestURL))
            {
                Content = content
            };

            using (var response = await _httpClient.SendAsync(httpRequestMessage))
            {
                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStringAsync();
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
