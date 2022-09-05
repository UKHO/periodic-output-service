using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureTableStorageHelper
    {
        void SaveHistory(WebJobHistory webJobHistory);
        DateTime GetSinceDateTime();
    }
}
