﻿using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Pks;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public class PksService : IPksService
    {
        private readonly ILogger<PksService> logger;
        private readonly IOptions<PksApiConfiguration> pksApiConfiguration;
        private readonly IAuthPksTokenProvider authPksTokenProvider;
        private readonly IPksApiClient pksApiClient;
        private const string KeysEnc = "/keys/ENC-S63";

        public PksService(ILogger<PksService> logger, IOptions<PksApiConfiguration> pksApiConfiguration, IAuthPksTokenProvider authPksTokenProvider, IPksApiClient pksApiClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.pksApiConfiguration = pksApiConfiguration ?? throw new ArgumentNullException(nameof(pksApiConfiguration));
            this.authPksTokenProvider = authPksTokenProvider ?? throw new ArgumentNullException(nameof(authPksTokenProvider));
            this.pksApiClient = pksApiClient ?? throw new ArgumentNullException(nameof(pksApiClient));
        }

        public async Task<List<ProductKeyServiceResponse>> PostProductKeyData(List<ProductKeyServiceRequest> productKeyServiceRequest, string? correlationId = null)
        {
            logger.LogInformation(EventIds.PostProductKeyDataToPksStarted.ToEventId(), "Request to post product key data to Product Key Service started | _X-Correlation-ID : {CorrelationId}", correlationId);

            string bodyJson;
            string uri = pksApiConfiguration.Value.BaseUrl + KeysEnc;
            string accessToken = await authPksTokenProvider.GetManagedIdentityAuthAsync(pksApiConfiguration.Value.ClientId, correlationId);

            string payloadJson = JsonConvert.SerializeObject(productKeyServiceRequest);

            HttpResponseMessage httpResponseMessage = await pksApiClient.PostPksDataAsync(uri, payloadJson, accessToken, correlationId);

            switch (httpResponseMessage.IsSuccessStatusCode)
            {
                case true:
                    {
                        bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        logger.LogInformation(EventIds.PostProductKeyDataToPksCompleted.ToEventId(), "Request to post product key data to Product Key Service completed | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), correlationId);

                        List<ProductKeyServiceResponse> productKeyServiceResponse = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(bodyJson);
                        return productKeyServiceResponse;
                    }
                default:
                    {
                        if (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
                        {
                            bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            logger.LogError(EventIds.PostProductKeyDataToPksFailed.ToEventId(), "Failed to retrieve post product key data with | StatusCode : {StatusCode}| Errors : {ErrorDetails} for Product Key Service | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), bodyJson, correlationId);

                            throw new FulfilmentException(EventIds.PostProductKeyDataToPksFailed.ToEventId());
                        }

                        logger.LogError(EventIds.PostProductKeyDataToPksFailed.ToEventId(), "Failed to post product key data | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), correlationId);
                        throw new FulfilmentException(EventIds.PostProductKeyDataToPksFailed.ToEventId());
                    }
            }
        }
    }
}
