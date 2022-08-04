using UKHO.PeriodicOutputService.Common.Enums;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssService
    {
        public Task<FssBatchStatus> CheckIfBatchCommitted(string url);
    }
}
