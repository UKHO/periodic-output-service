using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        void ProcessConfigs();
        bool CheckConfigFrequencyAndSaveQueueDetails(IList<BessConfig> configurationSettings);
    }
}
