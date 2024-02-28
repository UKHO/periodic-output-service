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
        void UpsertScheduleDetailEntities(DateTime nextSchedule, BessConfig configDetails, bool isExecuted);
        ScheduleDetails GetNextScheduleDetails(string name);
    }
}
