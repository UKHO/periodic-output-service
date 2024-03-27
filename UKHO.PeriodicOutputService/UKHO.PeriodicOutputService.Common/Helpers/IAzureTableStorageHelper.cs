using UKHO.PeriodicOutputService.Common.Models.Bess;
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

        Task UpsertScheduleDetailAsync(DateTime nextSchedule, BessConfig bessConfig, bool isExecuted);

        Task<ScheduleDetailEntity> GetScheduleDetailAsync(string configName);
    }
}
