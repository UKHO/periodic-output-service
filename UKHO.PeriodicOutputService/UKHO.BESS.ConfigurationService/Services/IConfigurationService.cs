using UKHO.PeriodicOutputService.Common.Models.BESS;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        List<ConfigurationSetting> ProcessConfigs();
        bool SaveBespokeDetailsToQueue(List<ConfigurationSetting> configurationSettings);
    }
}
