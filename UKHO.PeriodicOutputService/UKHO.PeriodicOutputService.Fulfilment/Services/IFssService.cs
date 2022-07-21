
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssService
    {
        public Task<FssBatchStatus> CheckIfBatchCommitted(string batchId);
        public Task<BatchDetail> GetBatchDetails(string batchId);
        public Task<Stream> DownloadFile(string downloadPath, string fileName, string fileLink);
    }
}
