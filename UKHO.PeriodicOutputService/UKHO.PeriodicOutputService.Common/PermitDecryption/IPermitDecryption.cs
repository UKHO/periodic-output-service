using UKHO.PeriodicOutputService.Common.Models;

namespace UKHO.PeriodicOutputService.Common.PermitDecryption
{
    public interface IPermitDecryption
    {
        PermitKey GetPermitKeys(string permit, string? correlationId = null);
    }
}
