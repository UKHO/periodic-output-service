using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private readonly ILogger<SalesCatalogueService> logger;
        private readonly IOptions<SalesCatalogueConfiguration> salesCatalogueConfig;
        private readonly IAuthScsTokenProvider authScsTokenProvider;
        private readonly ISalesCatalogueClient salesCatalogueClient;

        public SalesCatalogueService(ILogger<SalesCatalogueService> logger,
                                     IOptions<SalesCatalogueConfiguration> salesCatalogueConfig,
                                     IAuthScsTokenProvider authScsTokenProvider,
                                     ISalesCatalogueClient salesCatalogueClient)
        {
            this.logger = logger;
            this.salesCatalogueConfig = salesCatalogueConfig;
            this.authScsTokenProvider = authScsTokenProvider;
            this.salesCatalogueClient = salesCatalogueClient;
        }

        public async Task<SalesCatalogueDataResponse> GetSalesCatalogueData()
        {
            logger.LogInformation(EventIds.ScsGetSalesCatalogueDataRequestStarted.ToEventId(), "Get catalogue data from SCS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);

            var uri = $"{salesCatalogueConfig.Value.BaseUrl}/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/catalogue/{salesCatalogueConfig.Value.CatalogueType}";

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, null!, accessToken, uri);

            var response = await CreateSalesCatalogueDataResponse(httpResponse);

            logger.LogInformation(EventIds.ScsGetSalesCatalogueDataRequestCompleted.ToEventId(), "Get catalogue data from SCS completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return response;
        }

        public async Task<SalesCatalogueResponse> PostProductVersionsAsync(List<ProductVersion> productVersions)
        {
            logger.LogInformation(EventIds.ScsPostProductVersionsRequestStart.ToEventId(), "Post SCS for ProductVersions started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var accessToken = await authScsTokenProvider.GetManagedIdentityAuthAsync(salesCatalogueConfig.Value.ResourceId);

            var uri = $"{salesCatalogueConfig.Value.BaseUrl}/{salesCatalogueConfig.Value.Version}/productData/{salesCatalogueConfig.Value.ProductType}/products/productVersions";

            var payloadJson = JsonConvert.SerializeObject(productVersions);

            var httpResponse = await salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            var response = await CreateSalesCatalogueServiceResponse(httpResponse);

            logger.LogInformation(EventIds.ScsPostProductVersionsRequestCompleted.ToEventId(), "Post SCS for ProductVersions completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return response;
        }

        private async Task<SalesCatalogueDataResponse> CreateSalesCatalogueDataResponse(HttpResponseMessage httpResponse)
        {
            var response = new SalesCatalogueDataResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError(EventIds.ScsGetSalesCatalogueDataNonOkResponse.ToEventId(),
                                "Request to Sales Catalogue Service catalogue end point with uri:{RequestUri} FAILED.| {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}",
                                httpResponse.RequestMessage?.RequestUri, DateTime.UtcNow, httpResponse.StatusCode, CommonHelper.CorrelationID);
                response.ResponseCode = httpResponse.StatusCode;
                response.ResponseBody = null!;
                throw new FulfilmentException(EventIds.ScsGetSalesCatalogueDataNonOkResponse.ToEventId());
            }

            response.ResponseCode = httpResponse.StatusCode;
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                response.ResponseBody = JsonConvert.DeserializeObject<List<SalesCatalogueDataProductResponse>>(body)!;
            }

            if (httpResponse.Content.Headers.LastModified.HasValue)
            {
                response.LastModified = httpResponse.Content.Headers.LastModified.Value.UtcDateTime;
            }

            return response;
        }

        private async Task<SalesCatalogueResponse> CreateSalesCatalogueServiceResponse(HttpResponseMessage httpResponse)
        {
            var response = new SalesCatalogueResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NotModified)
            {
                logger.LogError(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId(), "Error in sales catalogue service with uri:{RequestUri} and responded with {StatusCode} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, CommonHelper.CorrelationID);
                response.ResponseCode = httpResponse.StatusCode;
                response.ResponseBody = null;
            }
            else
            {
                response.ResponseCode = httpResponse.StatusCode;
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    response.ResponseBody = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(body)!;
                }
            }

            return response;
        }
    }
}
