using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public class PksService : IPksService
    {
        private readonly ILogger<PksService> logger;
        private readonly PksApiConfiguration pksApiConfiguration;
        private readonly IAuthPksTokenProvider authPksTokenProvider;
        private readonly IPksApiClient pksApiClient;


        public PksService(ILogger<PksService> logger, IOptions<PksApiConfiguration> pksApiConfiguration, IAuthPksTokenProvider authPksTokenProvider, IPksApiClient pksApiClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.pksApiConfiguration = pksApiConfiguration.Value ?? throw new ArgumentNullException(nameof(pksApiConfiguration));
            this.authPksTokenProvider = authPksTokenProvider ?? throw new ArgumentNullException(nameof(authPksTokenProvider));
            this.pksApiClient = pksApiClient ?? throw new ArgumentNullException(nameof(pksApiClient));
        }

        public async Task<List<ProductKeyServiceResponse>> PostProductKeyData(List<ProductKeyServiceRequest> productKeyServiceRequest)
        {
            logger.LogInformation(EventIds.PostProductKeyDataToPksStarted.ToEventId(), "Request to post product key data to product key service started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            string uri = $"{pksApiConfiguration.BaseUrl}/keys/ENC-S63";
            string accessToken = await authPksTokenProvider.GetManagedIdentityAuthForPksAsync(pksApiConfiguration.ClientId);
            string payloadJson = JsonConvert.SerializeObject(productKeyServiceRequest);

            HttpResponseMessage httpResponseMessage = await pksApiClient.PostPksDataAsync(HttpMethod.Post, payloadJson, accessToken, uri);

            string bodyJson = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                logger.LogInformation(EventIds.PostProductKeyDataToPksCompleted.ToEventId(), "Request to post product key data to product key service completed | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), CommonHelper.CorrelationID);

                List<ProductKeyServiceResponse> productKeyServiceResponse = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(bodyJson);
                return productKeyServiceResponse;
            }
            else
            {
                if (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    logger.LogError(EventIds.PostProductKeyDataToPksFailed.ToEventId(), "Failed to post product key data | StatusCode : {StatusCode}| Errors : {ErrorDetails} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), bodyJson, CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.PostProductKeyDataToPksFailed.ToEventId());
                }
                else
                {
                    logger.LogError(EventIds.PostProductKeyDataToPksFailed.ToEventId(), "Failed to post product key data | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", httpResponseMessage.StatusCode.ToString(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.PostProductKeyDataToPksFailed.ToEventId());
                }
            }
        }
    }
}
