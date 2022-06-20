using System.Xml;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;

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

        public async Task<FleetManagerGetCatalogueResponse> GetCatalogue(string accessToken)
        {
            List<string> productIdentifiers = new();
            FleetManagerGetCatalogueResponse fleetManagerGetCatalogueResponse = new();
            
            HttpResponseMessage httpResponse = await _fleetManagerClient.GetCatalogue(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, accessToken, _fleetManagerB2BApiConfig.Value.SubscriptionKey);

            if (httpResponse.IsSuccessStatusCode)
            {
                using (Stream stream = httpResponse.Content.ReadAsStream())
                {
                    XmlReaderSettings settings = new();
                    settings.Async = true;
                    settings.IgnoreWhitespace = true;

                    using (XmlReader reader = XmlReader.Create(stream, settings))
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.Name == "ShortName")
                            {
                                reader.Read();
                                if (reader.HasValue) productIdentifiers.Add(reader.Value);
                            }
                        }
                    }
                }
            }
            fleetManagerGetCatalogueResponse.StatusCode = httpResponse.StatusCode;
            fleetManagerGetCatalogueResponse.ProductIdentifiers = productIdentifiers;
            return fleetManagerGetCatalogueResponse;
        }

        public async Task<FleetMangerGetAuthTokenResponse> GetJwtAuthJwtToken(string accessToken)
        {
            string responseContent = string.Empty;
            FleetMangerGetAuthTokenResponse fleetMangerGetAuthTokenResponse = new FleetMangerGetAuthTokenResponse();

            HttpResponseMessage httpResponse = await _fleetManagerClient.GetJwtAuthJwtToken(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, accessToken, _fleetManagerB2BApiConfig.Value.SubscriptionKey);
            fleetMangerGetAuthTokenResponse.StatusCode = httpResponse.StatusCode;

            if (httpResponse.IsSuccessStatusCode)
            {
                responseContent = CommonHelper.ExtractAccessToken(await httpResponse.Content.ReadAsStringAsync());
                fleetMangerGetAuthTokenResponse.AuthToken = responseContent;
            }

            return fleetMangerGetAuthTokenResponse;
        }

        public async Task<FleetMangerGetAuthTokenResponse> GetJwtAuthUnpToken()
        {
            string responseContent = string.Empty;
            FleetMangerGetAuthTokenResponse fleetMangerGetAuthTokenResponse = new FleetMangerGetAuthTokenResponse();

            string base64Credentials = CommonHelper.GetBase64EncodedCredentials(_fleetManagerB2BApiConfig.Value.UserName, _fleetManagerB2BApiConfig.Value.Password);

            HttpResponseMessage httpResponse = await _fleetManagerClient.GetJwtAuthUnpToken(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, base64Credentials, _fleetManagerB2BApiConfig.Value.SubscriptionKey);
            fleetMangerGetAuthTokenResponse.StatusCode = httpResponse.StatusCode;
            if (httpResponse.IsSuccessStatusCode)
            {
                responseContent = CommonHelper.ExtractAccessToken(await httpResponse.Content.ReadAsStringAsync());
                fleetMangerGetAuthTokenResponse.AuthToken = responseContent;
            }

            return fleetMangerGetAuthTokenResponse;
        }
    }
}
