using UKHO.PeriodicOutputService.Common.Models.Scs.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface ISalesCatalogueService
    {
        public Task<SalesCatalogueDataResponse> GetSalesCatalogueData();
    }
}
