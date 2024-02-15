using UKHO.PeriodicOutputService.Common.Models.BESS;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        Task<List<ConfigurationSetting>> ReadConfigurationJsonFiles();
    }
}
