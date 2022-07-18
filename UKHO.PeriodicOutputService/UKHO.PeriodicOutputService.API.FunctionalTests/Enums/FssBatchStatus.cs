namespace UKHO.PeriodicOutputService.API.FunctionalTests.Enums
{
    public enum FssBatchStatus
    {
        Incomplete = 1,
        CommitInProgress = 2,
        Committed = 3,
        Rolledback = 4,
        Failed = 5,
        Deleted = 6
    }
}
