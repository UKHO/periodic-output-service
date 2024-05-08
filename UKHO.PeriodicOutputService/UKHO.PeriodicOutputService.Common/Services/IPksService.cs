using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IPksService
    {
        Task<List<ProductKeyServiceResponse>> PostProductKeyData(List<ProductKeyServiceRequest> productKeyServiceRequest);
    }
}
