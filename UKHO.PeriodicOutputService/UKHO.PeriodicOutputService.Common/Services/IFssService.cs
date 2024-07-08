using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IFssService
    {
        public Task<FssBatchStatus> CheckIfBatchCommitted(string batchId, RequestType requestType, string? correlationId = null);
        public Task<GetBatchResponseModel> GetBatchDetails(string batchId, string? correlationId = null);
        public Task<bool> DownloadFileAsync(string fileName, string fileLink, long fileSize, string filePath, string? correlationId = null);
        public Task<string> CreateBatch(Batch batchType);
        public Task<string> CreateBatch(Batch batchType, ConfigQueueMessage configQueueMessage, string? correlationId = null);
        public Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength, string mimeType, Batch batchType, string? correlationId = null);
        public Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo, string? correlationId = null);
        public Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds, string? correlationId = null);
        public Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames, Batch batchType, string? correlationId = null);
        Task<IEnumerable<BatchFile>> GetAioInfoFolderFilesAsync(string batchId, string correlationId);
        Task<string> SearchReadMeFilePathAsync(string correlationId, string readMeSearchFilter);
        Task<bool> DownloadReadMeFileAsync(string readMeFilePath, string exchangeSetRootPath, string correlationId);
    }
}
