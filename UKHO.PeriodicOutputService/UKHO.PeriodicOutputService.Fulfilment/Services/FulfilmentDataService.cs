using System.Net;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IExchangeSetApiService _exchangeSetApiService;
        private readonly IFssBatchService _fssBatchService;
        private readonly ILogger<FulfilmentDataService> _logger;

        public FulfilmentDataService(IFleetManagerService fleetManagerService,
                                     IExchangeSetApiService exchangeSetApiService,
                                     IFssBatchService fssBatchService,
                                     ILogger<FulfilmentDataService> logger)
        {
            _fleetManagerService = fleetManagerService;
            _exchangeSetApiService = exchangeSetApiService;
            _fssBatchService = fssBatchService;
            _logger = logger;
        }

        public async Task<string> CreatePosExchangeSet()
        {
            var tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                var catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    var response = await _exchangeSetApiService.PostProductIdentifiersData(catalogueResponse.ProductIdentifiers);
                    FssBatchStatus fssBatchStatus = await _fssBatchService.CheckIfBatchCommitted(response.Links.ExchangeSetBatchStatusUri.Href);
                    return "Success";
                }
            }
            return "Fail";
        }
    }
}
