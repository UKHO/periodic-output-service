﻿using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public class FssService : IFssService
    {
        private readonly IOptions<FssApiConfiguration> _fssApiConfiguration;
        private readonly ILogger<FssService> _logger;
        private readonly IFssApiClient _fssApiClient;
        private readonly IAuthFssTokenProvider _authFssTokenProvider;
        private readonly IFileSystemHelper _fileSystemHelper;
        private readonly IConfiguration _configuration;

        public FssService(ILogger<FssService> logger,
                               IOptions<FssApiConfiguration> fssApiConfiguration,
                               IFssApiClient fssApiClient,
                               IAuthFssTokenProvider authFssTokenProvider,
                               IFileSystemHelper fileSystemHelper,
                               IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fssApiConfiguration = fssApiConfiguration ?? throw new ArgumentNullException(nameof(fssApiConfiguration));
            _fssApiClient = fssApiClient ?? throw new ArgumentNullException(nameof(fssApiClient));
            _authFssTokenProvider = authFssTokenProvider ?? throw new ArgumentNullException(nameof(authFssTokenProvider));
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId)
        {
            _logger.LogInformation(EventIds.FssBatchStatusPollingStarted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            FssBatchStatus[] pollBatchStatus = { FssBatchStatus.CommitInProgress, FssBatchStatus.Incomplete };

            while (pollBatchStatus.Contains(batchStatus) &&
                        DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime)))
            {
                _logger.LogInformation(EventIds.GetBatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken);

                if (batchStatusResponse.IsSuccessStatusCode)
                {
                    FssBatchStatusResponseModel fssBatchStatusResponseModel = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                    Enum.TryParse(fssBatchStatusResponseModel?.Status, false, out batchStatus);

                    _logger.LogInformation(EventIds.GetBatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);

                    await Task.Delay(int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTime));
                }
                else
                {
                    _logger.LogError(EventIds.GetBatchStatusRequestFailed.ToEventId(), "Request to get batch status for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.GetBatchStatusRequestFailed.ToEventId());
                }
            }

            if (pollBatchStatus.Contains(batchStatus))
            {
                _logger.LogError(EventIds.FssBatchStatusPollingTimedOut.ToEventId(), "Fss batch status polling timed out for BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.FssBatchStatusPollingTimedOut.ToEventId());
            }

            _logger.LogInformation(EventIds.FssBatchStatusPollingCompleted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return batchStatus;
        }

        public async Task<GetBatchResponseModel> GetBatchDetails(string batchId)
        {
            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchID} from FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage batchDetailResponse = await _fssApiClient.GetBatchDetailsAsync(uri, accessToken);

            if (batchDetailResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.GetBatchDetailRequestCompleted.ToEventId(), "Request to get batch details for BatchID - {BatchID} from FSS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return JsonConvert.DeserializeObject<GetBatchResponseModel>(await batchDetailResponse.Content.ReadAsStringAsync());
            }
            else
            {
                _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Request to get batch details for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.GetBatchDetailRequestFailed.ToEventId());
            }
        }

        public async Task<bool> DownloadFile(string fileName, string fileLink, long fileSize, string filePath)
        {
            long startByte = 0;
            long downloadSize = fileSize < 10485760 ? fileSize : 10485760;
            long endByte = downloadSize;

            _logger.LogInformation(EventIds.DownloadFileStarted.ToEventId(), "Downloading of file {fileName} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(fileLink, accessToken);

            string rangeHeader = String.Empty;

            if (fileDownloadResponse.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                uri = fileDownloadResponse.Headers.GetValues("Location").FirstOrDefault();
                rangeHeader = $"bytes={startByte}-{endByte}";
            }

            while (startByte <= endByte)
            {

                fileDownloadResponse = await _fssApiClient.DownloadFile(uri, accessToken, rangeHeader);

                if (!fileDownloadResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading of file {fileName} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), fileDownloadResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.DownloadFileFailed.ToEventId());
                }

                Stream stream = fileDownloadResponse.Content.ReadAsStream();

                _fileSystemHelper.CreateFileCopy(filePath, stream);

                startByte = endByte + 1;
                endByte = endByte + downloadSize;

                if (endByte > fileSize - 1)
                {
                    endByte = fileSize - 1;
                }

                rangeHeader = $"bytes={startByte}-{endByte}";
            }
            _logger.LogInformation(EventIds.DownloadFileCompleted.ToEventId(), "Downloading of file {fileName} completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return true;
        }

        public async Task<string> CreateBatch(Batch batchType)
        {
            _logger.LogInformation(EventIds.CreateBatchStarted.ToEventId(), "Request to create batch for {BatchType} in FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchType, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string? uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            CreateBatchRequestModel createBatchRequest = CreateBatchRequestModel(batchType);
            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);
            HttpResponseMessage? httpResponse = await _fssApiClient.CreateBatchAsync(uri, payloadJson, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                CreateBatchResponseModel createBatchResponse = JsonConvert.DeserializeObject<CreateBatchResponseModel>(await httpResponse.Content.ReadAsStringAsync());
                _logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "New batch for {BatchType} created in FSS. Batch ID is {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, createBatchResponse.BatchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return createBatchResponse.BatchId;
            }
            else
            {
                _logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Request to create batch for {BatchType} in FSS failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.CreateBatchFailed.ToEventId());
            }
        }

        public async Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength, string mimeType)
        {
            _logger.LogInformation(EventIds.AddFileToBatchRequestStarted.ToEventId(), "Adding file {FileName} in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            AddFileToBatchRequestModel addFileRequest = CreateAddFileRequestModel(fileName);
            string payloadJson = JsonConvert.SerializeObject(addFileRequest);
            HttpResponseMessage httpResponseMessage = await _fssApiClient.AddFileToBatchAsync(uri, payloadJson, accessToken, fileLength, mimeType);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.AddFileToBatchRequestCompleted.ToEventId(), "File {FileName} is added in batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponseMessage.StatusCode.ToString(), CommonHelper.CorrelationID);
                return httpResponseMessage.StatusCode == HttpStatusCode.Created;
            }
            else
            {
                _logger.LogError(EventIds.AddFileToBatchRequestFailed.ToEventId(), "Request to add file {FileName} to batch with BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponseMessage.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.AddFileToBatchRequestFailed.ToEventId());
            }
        }

        public async Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo)
        {
            _logger.LogInformation(EventIds.UploadFileBlockStarted.ToEventId(), "Uploading of file blocks of {FileName} for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileInfo.Name, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            int fileBlockSize = _fssApiConfiguration.Value.BlockSizeInMultipleOfKBs;

            long blockSizeInMultipleOfKBs = fileBlockSize is <= 0 or > 4096
                ? 1024
                : fileBlockSize;
            long blockSize = blockSizeInMultipleOfKBs * 1024;
            List<string> blockIdList = new();
            List<Task> ParallelBlockUploadTasks = new();
            long uploadedBytes = 0;
            int blockNum = 0;

            while (uploadedBytes < fileInfo.Length)
            {
                blockNum++;
                int readBlockSize = (int)(fileInfo.Length - uploadedBytes <= blockSize ? fileInfo.Length - uploadedBytes : blockSize);
                string blockId = CommonHelper.GetBlockIds(blockNum);

                UploadFileBlockRequestModel uploadFileBlockRequestModel = new()
                {
                    BatchId = batchId,
                    BlockId = blockId,
                    FullFileName = fileInfo.FullName,
                    Offset = uploadedBytes,
                    Length = readBlockSize,
                    FileName = fileInfo.Name
                };

                ParallelBlockUploadTasks.Add(UploadFileBlock(uploadFileBlockRequestModel));
                blockIdList.Add(blockId);
                uploadedBytes += readBlockSize;
                //run uploads in parallel	
                if (ParallelBlockUploadTasks.Count >= _fssApiConfiguration.Value.ParallelUploadThreadCount)
                {
                    Task.WaitAll(ParallelBlockUploadTasks.ToArray());
                    ParallelBlockUploadTasks.Clear();
                }
            }
            if (ParallelBlockUploadTasks.Count > 0)
            {
                await Task.WhenAll(ParallelBlockUploadTasks);
                ParallelBlockUploadTasks.Clear();
            }

            _logger.LogInformation(EventIds.UploadFileBlockCompleted.ToEventId(), "Uploading of file blocks of {FileName} for BatchID - {BatchID} completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileInfo.Name, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return blockIdList;
        }

        public async Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds)
        {
            _logger.LogInformation(EventIds.WriteBlockToFileStarted.ToEventId(), "Writing blocks in file {FileName} for batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            WriteBlockFileRequestModel writeBlockfileRequestModel = new()
            {
                BlockIds = blockIds
            };

            string payloadJson = JsonConvert.SerializeObject(writeBlockfileRequestModel);
            HttpResponseMessage httpResponse = await _fssApiClient.WriteBlockInFileAsync(uri, payloadJson, accessToken, "application/octet-stream");

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.WriteBlockToFileCompleted.ToEventId(), "File blocks written in file {FileName} for batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return true;
            }
            else
            {
                _logger.LogError(EventIds.WriteBlockToFileFailed.ToEventId(), "Request to write blocks in file {FileName} failed for batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID.ToString());
                throw new FulfilmentException(EventIds.WriteBlockToFileFailed.ToEventId());
            }
        }

        public async Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames, Batch batchType)
        {
            _logger.LogInformation(EventIds.CommitBatchStarted.ToEventId(), "Batch commit for {BatchType} with BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchType, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID.ToString());

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            List<FileDetail> fileDetails = _fileSystemHelper.GetFileMD5(fileNames);
            BatchCommitRequestModel batchCommitRequestModel = new()
            {
                FileDetails = fileDetails
            };

            string payloadJson = JsonConvert.SerializeObject(batchCommitRequestModel.FileDetails);
            HttpResponseMessage httpResponse = await _fssApiClient.CommitBatchAsync(uri, payloadJson, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.CommitBatchCompleted.ToEventId(), "Batch {BatchType} with BatchID - {BatchID} committed in FSS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode, CommonHelper.CorrelationID.ToString());
                return true;
            }
            else
            {
                _logger.LogError(EventIds.CommitBatchFailed.ToEventId(), "Batch commit for {BatchType} failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode, CommonHelper.CorrelationID.ToString());
                throw new FulfilmentException(EventIds.AddFileToBatchRequestFailed.ToEventId());
            }
        }

        //Private Methods
        private CreateBatchRequestModel CreateBatchRequestModel(Batch batchType)
        {
            string currentYear = DateTime.UtcNow.Year.ToString();
            string currentWeek = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();

            CreateBatchRequestModel createBatchRequest = new()
            {
                BusinessUnit = _fssApiConfiguration.Value.BusinessUnit,
                ExpiryDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = string.IsNullOrEmpty(_fssApiConfiguration.Value.PosReadUsers) ? new() : _fssApiConfiguration.Value.PosReadUsers.Split(",").ToList(),
                    ReadGroups = string.IsNullOrEmpty(_fssApiConfiguration.Value.PosReadGroups) ? new() : _fssApiConfiguration.Value.PosReadGroups.Split(",").ToList(),
                },
                Attributes = new List<KeyValuePair<string, string>>
                {
                    new("Product Type", "AVCS"),
                    new("Week Number", currentWeek),
                    new("Year", currentYear),
                    new("Year / Week", currentYear + " / " + currentWeek)
                }
            };

            //This batch attribute is added for fss stub.
            if (bool.Parse(_configuration["IsFTRunning"]))
            {
                createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Batch Type", batchType.ToString()));
            }

            switch (batchType)
            {
                case Batch.PosFullAvcsIsoSha1Batch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Base"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Media Type", "DVD"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("S63 Version", "1.2"));
                    break;

                case Batch.PosFullAvcsZipBatch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Base"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("S63 Version", "1.2"));
                    break;

                case Batch.PosUpdateBatch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Update"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("S63 Version", "1.2"));
                    break;

                case Batch.PosCatalogueBatch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Catalogue Type", "XML"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Content", "Catalogue"));
                    break;

                case Batch.PosEncUpdateBatch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Content", "ENC Updates"));
                    break;

                default:
                    break;
            };
            return createBatchRequest;
        }

        private AddFileToBatchRequestModel CreateAddFileRequestModel(string fileName)
        {
            AddFileToBatchRequestModel addFileToBatchRequestModel = new()
            {
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new("Product Type", "AVCS"),
                    new("File Name", fileName)
                }
            };
            return addFileToBatchRequestModel;
        }

        private async Task UploadFileBlock(UploadFileBlockRequestModel uploadBlockMetaData)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{uploadBlockMetaData.BatchId}/files/{uploadBlockMetaData.FileName}/{uploadBlockMetaData.BlockId}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            byte[] blockBytes = _fileSystemHelper.GetFileInBytes(uploadBlockMetaData);
            byte[]? blockMd5Hash = CommonHelper.CalculateMD5(blockBytes);
            HttpResponseMessage httpResponse;
            httpResponse = await _fssApiClient.UploadFileBlockAsync(uri, blockBytes, blockMd5Hash, accessToken, "application/octet-stream");

            if (httpResponse.IsSuccessStatusCode)
            {
                await Task.CompletedTask;
            }
            else
            {
                _logger.LogError(EventIds.UploadFileBlockFailed.ToEventId(), "Request to upload block {BlockID} of {FileName} failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", uploadBlockMetaData.BlockId, uploadBlockMetaData.FileName, uploadBlockMetaData.BatchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID.ToString());
                throw new FulfilmentException(EventIds.UploadFileBlockFailed.ToEventId());
            }
        }
    }
}