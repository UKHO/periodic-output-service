using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        IList<BessConfig> ProcessConfigs();
        bool ScheduleConfigDetails(IList<BessConfig> configurationSettings);
    }
}
