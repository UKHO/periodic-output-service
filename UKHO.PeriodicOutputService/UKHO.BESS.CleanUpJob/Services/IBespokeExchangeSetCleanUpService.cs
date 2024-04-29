namespace UKHO.BESS.CleanUpJob.Services
{
    public interface IBespokeExchangeSetCleanUpService
    {
        Task DeleteHistoricFoldersAndFiles();
    }
}
