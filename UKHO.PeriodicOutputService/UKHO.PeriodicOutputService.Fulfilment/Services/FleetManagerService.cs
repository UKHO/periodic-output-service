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

        public async Task<FleetMangerGetAuthTokenResponse> GetJwtAuthUnpToken()
        {
            string jwtAuthUnpToken = string.Empty;

            string base64Credentials = CommonHelper.GetBase64EncodedCredentials(_fleetManagerB2BApiConfig.Value.UserName, _fleetManagerB2BApiConfig.Value.Password);

            HttpResponseMessage httpResponse = await _fleetManagerClient.GetJwtAuthUnpToken(HttpMethod.Get, _fleetManagerB2BApiConfig.Value.BaseUrl, base64Credentials, _fleetManagerB2BApiConfig.Value.SubscriptionKey);

            if (httpResponse.IsSuccessStatusCode)
            {
                jwtAuthUnpToken = CommonHelper.ExtractAccessToken(await httpResponse.Content.ReadAsStringAsync());
            }

            return new FleetMangerGetAuthTokenResponse() { StatusCode = httpResponse.StatusCode, AuthToken = jwtAuthUnpToken };
        }

        public async Task<FleetManagerGetCatalogueResponse> GetCatalogue(string accessToken)
        {
            List<string> productIdentifiers = new();

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
            return new FleetManagerGetCatalogueResponse() { StatusCode = httpResponse.StatusCode, ProductIdentifiers = productIdentifiers };
        }
    }
}
