using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IFleetManagerService _fleetManagerService;
        private readonly IExchangeSetApiService _exchangeSetApiService;
        private readonly IFssBatchService _fssBatchService;

        public FulfilmentDataService(IFleetManagerService fleetManagerService, IExchangeSetApiService exchangeSetApiService, IFssBatchService fssBatchService)
        {
            _fleetManagerService = fleetManagerService;
            _exchangeSetApiService = exchangeSetApiService;
            _fssBatchService = fssBatchService;
        }

        public async Task<string> CreatePosExchangeSet()
        {
            FleetMangerGetAuthTokenResponse tokenResponse = await _fleetManagerService.GetJwtAuthUnpToken();

            if (!string.IsNullOrEmpty(tokenResponse.AuthToken))
            {
                FleetManagerGetCatalogueResponse catalogueResponse = await _fleetManagerService.GetCatalogue(tokenResponse.AuthToken);

                if (catalogueResponse != null && catalogueResponse.ProductIdentifiers != null && catalogueResponse.ProductIdentifiers.Count > 0)
                {
                    var response = await _exchangeSetApiService.PostProductIdentifiersData(catalogueResponse.ProductIdentifiers);

                    if (response != null)
                    {
                        string batchStatus = await _fssBatchService.CheckIfBatchCommitted(response.Links.ExchangeSetBatchStatusUri.Href);
                    }

                    return "Fleet Manager full AVCS ProductIdentifiers received";
                }
            }
            return "Fleet Manager full AVCS ProductIdentifiers not received";
        }
    }
}
