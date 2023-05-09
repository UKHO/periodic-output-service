using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IFssService
    {
        public Task<FssBatchStatus> CheckIfBatchCommitted(string batchId, RequestType requestType);
        public Task<GetBatchResponseModel> GetBatchDetails(string batchId);
        public Task<bool> DownloadFileAsync(string fileName, string fileLink, long fileSize, string filePath);
        public Task<string> CreateBatch(Batch batchType);
        public Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength, string mimeType, Batch batchType);
        public Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo);
        public Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds);
        public Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames, Batch batchType);
        Task<IEnumerable<BatchFile>> GetAioInfoFolderFilesAsync(string batchId, string correlationId);
    }
}
