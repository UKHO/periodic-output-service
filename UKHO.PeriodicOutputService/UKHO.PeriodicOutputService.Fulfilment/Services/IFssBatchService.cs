
using UKHO.PeriodicOutputService.Common.Enums;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssBatchService
    {
        public Task<FssBatchStatus> CheckIfBatchCommitted(string url);
    }
}
