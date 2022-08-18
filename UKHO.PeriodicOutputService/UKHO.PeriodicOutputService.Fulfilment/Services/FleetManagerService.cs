using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FleetManagerService : IFleetManagerService
    {
        private readonly IOptions<FleetManagerApiConfiguration> _fleetManagerApiConfiguration;
        private readonly IFleetManagerApiClient _fleetManagerClient;
        private readonly ILogger<FleetManagerService> _logger;

        public FleetManagerService(IOptions<FleetManagerApiConfiguration> fleetManagerApiConfiguration,
                                   IFleetManagerApiClient fleetManagerClient,
                                   ILogger<FleetManagerService> logger)
        {
            _fleetManagerApiConfiguration = fleetManagerApiConfiguration;
            _fleetManagerClient = fleetManagerClient;
            _logger = logger;
        }

        public async Task<FleetMangerGetAuthTokenResponseModel> GetJwtAuthUnpToken()
        {
            _logger.LogInformation(EventIds.GetFleetMangerAuthTokenStarted.ToEventId(), "Getting auth token from fleet manager started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string base64Credentials = CommonHelper.GetBase64EncodedCredentials(_fleetManagerApiConfiguration.Value.UserName, _fleetManagerApiConfiguration.Value.Password);

            HttpResponseMessage httpResponse = await _fleetManagerClient.GetJwtAuthUnpToken(HttpMethod.Get, _fleetManagerApiConfiguration.Value.BaseUrl, base64Credentials, _fleetManagerApiConfiguration.Value.SubscriptionKey);

            if (httpResponse.IsSuccessStatusCode)
            {
                string jwtAuthUnpToken = CommonHelper.ExtractAccessToken(await httpResponse.Content.ReadAsStringAsync());

                _logger.LogInformation(EventIds.GetFleetMangerAuthTokenCompleted.ToEventId(), "Getting auth token from fleet manager completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return new FleetMangerGetAuthTokenResponseModel() { StatusCode = httpResponse.StatusCode, AuthToken = jwtAuthUnpToken };
            }
            else
            {
                _logger.LogError(EventIds.GetFleetMangerAuthTokenFailed.ToEventId(), "Failed to get auth token from fleet manager at {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.GetFleetMangerAuthTokenFailed.ToEventId());
            }
        }

        public async Task<FleetManagerGetCatalogueResponseModel> GetCatalogue(string accessToken)
        {
            _logger.LogInformation(EventIds.GetFleetMangerCatalogueStarted.ToEventId(), "Getting catalogue from fleet manager started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            HttpResponseMessage httpResponse = await _fleetManagerClient.GetCatalogue(HttpMethod.Get, _fleetManagerApiConfiguration.Value.BaseUrl, accessToken, _fleetManagerApiConfiguration.Value.SubscriptionKey);

            if (httpResponse.IsSuccessStatusCode)
            {
                List<string> productIdentifiers = new();
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
                                if (reader.HasValue)
                                {
                                    productIdentifiers.Add(reader.Value);
                                }
                            }
                        }
                    }
                }
                _logger.LogInformation(EventIds.GetFleetMangerCatalogueCompleted.ToEventId(), "Getting catalogue from fleet manager completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return new FleetManagerGetCatalogueResponseModel() { StatusCode = httpResponse.StatusCode, ProductIdentifiers = productIdentifiers };
            }
            else
            {
                _logger.LogError(EventIds.GetFleetMangerCatalogueFailed.ToEventId(), "Failed to get catalogue from fleet manager | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.GetFleetMangerCatalogueFailed.ToEventId());
            }
        }
    }
}
