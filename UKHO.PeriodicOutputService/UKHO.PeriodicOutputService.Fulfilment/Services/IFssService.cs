using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssService
    {
        public Task<FssBatchStatus> CheckIfBatchCommitted(string batchId);
        public Task<GetBatchResponseModel> GetBatchDetails(string batchId);
        public Task<bool> DownloadFile(string fileName, string fileLink, long fileSize, string filePath);
        public Task<string> CreateBatch(Batch batchType);
        public Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength, string mimeType);
        public Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo);
        public Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds);
        public Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames, Batch batchType);
    }
}
