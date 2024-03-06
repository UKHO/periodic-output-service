using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationService
    {
        void ProcessConfigs();

        Task<bool> CheckConfigFrequencyAndSaveQueueDetails(IList<BessConfig> bessConfigs);
    }
}
