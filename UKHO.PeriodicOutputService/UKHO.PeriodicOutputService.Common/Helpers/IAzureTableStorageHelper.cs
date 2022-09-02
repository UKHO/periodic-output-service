namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureTableStorageHelper
    {
        void SaveEntity(Task[] tasks, DateTime nextSchedule);
        DateTime GetSinceDateTime();
    }
}
