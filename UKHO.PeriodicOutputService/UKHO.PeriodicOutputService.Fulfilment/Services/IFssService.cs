using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Enums;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssService
    {
        public Task<string> CreateBatch(string mediaType);
        public Task<FssBatchStatus> CheckIfBatchCommitted(string batchId);
        public Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength);
        public Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo);
        public Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds);
        public Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames);
    }
}
