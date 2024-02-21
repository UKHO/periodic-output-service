using UKHO.PeriodicOutputService.Common.Models.BESS;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureTableStorageHelper
    {
        void SaveHistory(WebJobHistory webJobHistory);
        DateTime GetSinceDateTime();
        void SaveProductVersionDetails(List<ProductVersion> productVersions);
        List<ProductVersionEntities> GetLatestProductVersionDetails();
        void RefreshNextSchedule(DateTime nextSchedule, ConfigurationSetting configurationSetting, bool IsExecuted);
        ScheduleDetails GetNextScheduleDetails(string name);
    }
}
