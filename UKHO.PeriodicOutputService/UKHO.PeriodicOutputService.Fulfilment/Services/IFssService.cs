
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssService
    {
        public Task<string> CreateBatch();
        public Task<FssBatchStatus> CheckIfBatchCommitted(string batchId);
        public Task<GetBatchResponseModel> GetBatchDetails(string batchId);
        public Task<Stream> DownloadFile(string downloadPath, string fileName, string fileLink);
        public Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength);
        public Task<List<string>> UploadBlocks(string batchId, FileInfo fileInfo);
        public Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds);
    }
}
