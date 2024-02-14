using UKHO.BESS.ConfigurationService.Models;

namespace UKHO.BESS.ConfigurationService.Services
{
    public interface IConfigurationFileReaderService
    {
        Task<List<ConfigurationSetting>> ReadConfigurationJsonFiles();
    }
}
