using UKHO.PeriodicOutputService.Common.Helpers;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FleetManagerService : IFleetManagerService
    {
        private readonly IOptions<FleetManagerB2BApiConfiguration> _fleetManagerB2BApiConfig;
        private readonly IFleetManagerClient _fleetManagerClient;

        public FleetManagerService(IOptions<FleetManagerB2BApiConfiguration> fleetManagerB2BApiConfig,
                                   IFleetManagerClient fleetManagerClient)
        {
            _fleetManagerB2BApiConfig = fleetManagerB2BApiConfig;
            _fleetManagerClient = fleetManagerClient;
        }

        public async Task<string> GetCatalogue(string accessToken)
        {
            HttpResponseMessage httpResponse = await _fleetManagerClient.GetCatalogue(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, accessToken, _fleetManagerB2BApiConfig.Value.SubscriptionKey);

            return await httpResponse.Content.ReadAsStringAsync();
        }

        public async Task<string> GetJwtAuthJwtToken(string accessToken)
        {
            string responseContent = string.Empty;
            HttpResponseMessage httpResponse = await _fleetManagerClient.GetJwtAuthJwtToken(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, accessToken, _fleetManagerB2BApiConfig.Value.SubscriptionKey);

            if (httpResponse.IsSuccessStatusCode)
            {
                responseContent = await httpResponse.Content.ReadAsStringAsync();
                responseContent = responseContent.Split(",")[0].Split(":")[1].Remove(0, 1).Replace("\"", "");
            }
            return responseContent;
        }

        public async Task<string> GetJwtAuthUnpToken()
        {
            string responseContent = string.Empty;
            var base64Credentials = CommonHelper.Base64Encode(_fleetManagerB2BApiConfig.Value.UserName, _fleetManagerB2BApiConfig.Value.Password);

            HttpResponseMessage httpResponse = await _fleetManagerClient.GetJwtAuthUnpToken(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, base64Credentials, _fleetManagerB2BApiConfig.Value.SubscriptionKey);

            if (httpResponse.IsSuccessStatusCode)
            {
                responseContent = await httpResponse.Content.ReadAsStringAsync();
                responseContent = responseContent.Split(",")[0].Split(":")[1].Remove(0, 1).Replace("\"", "");
            }
            return responseContent;
        }
    }
}
