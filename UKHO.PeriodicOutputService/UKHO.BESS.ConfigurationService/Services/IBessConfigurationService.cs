using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IBessConfigurationService
    {
        List<BessConfig> ProcessConfigs();
    }
}
