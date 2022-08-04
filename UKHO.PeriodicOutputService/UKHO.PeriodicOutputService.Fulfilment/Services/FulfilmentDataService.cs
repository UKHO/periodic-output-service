using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IEssService _essService;
        private readonly IFssService _fssService;
        private readonly ILogger<FulfilmentDataService> _logger;

        public FulfilmentDataService(IFleetManagerService fleetManagerService,
                                     IEssService essService,
                                     IFssService fssService,
                                     ILogger<FulfilmentDataService> logger)
        {
            _fleetManagerService = fleetManagerService;
            _essService = essService;
            _fssService = fssService;
            _logger = logger;
        }

        public async Task<string> CreatePosExchangeSets()
        {
            var tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                var catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    var response = await _essService.PostProductIdentifiersData(catalogueResponse.ProductIdentifiers);
                    FssBatchStatus fssBatchStatus = await _fssService.CheckIfBatchCommitted(response.Links.ExchangeSetBatchStatusUri.Href);
                    return "Success";
                }
            }
            return "Fail";
        }
    }
}
