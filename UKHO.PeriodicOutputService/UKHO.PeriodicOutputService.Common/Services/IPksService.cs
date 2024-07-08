using UKHO.PeriodicOutputService.Common.Models.Pks;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IPksService
    {
        Task<List<ProductKeyServiceResponse>> PostProductKeyData(List<ProductKeyServiceRequest> productKeyServiceRequest, string? correlationId = null);
    }
}
