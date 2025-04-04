﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

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
        private readonly IAzureTableStorageHelper _azureTableStorageHelper;
        private const string ServerHeaderValue = "Windows-Azure-Blob";

        public FssService(ILogger<FssService> logger,
                               IOptions<FssApiConfiguration> fssApiConfiguration,
                               IFssApiClient fssApiClient,
                               IAuthFssTokenProvider authFssTokenProvider,
                               IFileSystemHelper fileSystemHelper,
                               IConfiguration configuration,
                               IAzureTableStorageHelper azureTableStorageHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fssApiConfiguration = fssApiConfiguration ?? throw new ArgumentNullException(nameof(fssApiConfiguration));
            _fssApiClient = fssApiClient ?? throw new ArgumentNullException(nameof(fssApiClient));
            _authFssTokenProvider = authFssTokenProvider ?? throw new ArgumentNullException(nameof(authFssTokenProvider));
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId, RequestType requestType, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.FssBatchStatusPollingStarted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;
            double batchStatusPollingCutoffTime;
            int batchStatusPollingDelayTime;
            if (requestType.Equals(RequestType.POS))
            {
                batchStatusPollingCutoffTime = double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime);
                batchStatusPollingDelayTime = int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTime);
            }
            else if (requestType.Equals(RequestType.BESS))
            {
                batchStatusPollingCutoffTime = double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTimeForBESS);
                batchStatusPollingDelayTime = int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTimeForBESS);
            }
            else
            {
                batchStatusPollingCutoffTime = double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTimeForAIO);
                batchStatusPollingDelayTime = int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTimeForAIO);
            }

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            FssBatchStatus[] pollBatchStatus = { FssBatchStatus.CommitInProgress, FssBatchStatus.Incomplete };

            while (pollBatchStatus.Contains(batchStatus) &&
                        DateTime.UtcNow - startTime < TimeSpan.FromMinutes(batchStatusPollingCutoffTime))
            {
                _logger.LogInformation(EventIds.GetBatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken, correlationId);

                if (batchStatusResponse.IsSuccessStatusCode)
                {
                    FssBatchStatusResponseModel? fssBatchStatusResponseModel = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                    Enum.TryParse(fssBatchStatusResponseModel?.Status, false, out batchStatus);

                    _logger.LogInformation(EventIds.GetBatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));

                    await Task.Delay(batchStatusPollingDelayTime);
                }
                else
                {
                    _logger.LogError(EventIds.GetBatchStatusRequestFailed.ToEventId(), "Request to get batch status for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                    throw new FulfilmentException(EventIds.GetBatchStatusRequestFailed.ToEventId());
                }
            }

            if (pollBatchStatus.Contains(batchStatus))
            {
                _logger.LogError(EventIds.FssBatchStatusPollingTimedOut.ToEventId(), "Fss batch status polling timed out for BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));
                throw new FulfilmentException(EventIds.FssBatchStatusPollingTimedOut.ToEventId());
            }

            _logger.LogInformation(EventIds.FssBatchStatusPollingCompleted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));
            return batchStatus;
        }

        public async Task<GetBatchResponseModel> GetBatchDetails(string batchId, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchID} from FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            HttpResponseMessage batchDetailResponse = await _fssApiClient.GetBatchDetailsAsync(uri, accessToken, correlationId);

            if (batchDetailResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.GetBatchDetailRequestCompleted.ToEventId(), "Request to get batch details for BatchID - {BatchID} from FSS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                return JsonConvert.DeserializeObject<GetBatchResponseModel>(await batchDetailResponse.Content.ReadAsStringAsync());
            }

            _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Request to get batch details for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
            throw new FulfilmentException(EventIds.GetBatchDetailRequestFailed.ToEventId());
        }

        public async Task<bool> DownloadFileAsync(string fileName, string fileLink, long fileSize, string filePath, string? correlationId = null)
        {
            long startByte = 0;
            long downloadSize = fileSize < 10485760 ? fileSize : 10485760;
            long endByte = downloadSize;

            _logger.LogInformation(EventIds.DownloadFileStarted.ToEventId(), "Downloading of file {fileName} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(fileLink, accessToken);

            string rangeHeader = string.Empty;

            if (fileDownloadResponse.StatusCode == HttpStatusCode.TemporaryRedirect)
            {
                uri = fileDownloadResponse.Headers.GetValues("Location").FirstOrDefault() ?? throw new FulfilmentException(EventIds.DownloadFileFailed.ToEventId());
                rangeHeader = $"bytes={startByte}-{endByte}";
            }

            while (startByte <= endByte)
            {
                fileDownloadResponse = await _fssApiClient.DownloadFile(uri, accessToken, rangeHeader, correlationId);

                if (!fileDownloadResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading of file {fileName} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), fileDownloadResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                    throw new FulfilmentException(EventIds.DownloadFileFailed.ToEventId());
                }

                Stream stream = fileDownloadResponse.Content.ReadAsStream();

                _fileSystemHelper.CreateFileCopy(filePath, stream);

                startByte = endByte + 1;
                endByte += downloadSize;

                if (endByte > fileSize - 1)
                {
                    endByte = fileSize - 1;
                }

                rangeHeader = $"bytes={startByte}-{endByte}";
            }
            _logger.LogInformation(EventIds.DownloadFileCompleted.ToEventId(), "Downloading of file {fileName} completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));
            return true;
        }

        public async Task<string> CreateBatch(Batch batchType, ConfigQueueMessage configQueueMessage, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.CreateBatchStarted.ToEventId(), "Request to create batch for {BatchType} in FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchType, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string? uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            CreateBatchRequestModel createBatchRequest = CreateBatchRequestModelForBess(batchType, configQueueMessage);
            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);
            HttpResponseMessage? httpResponse = await _fssApiClient.CreateBatchAsync(uri, payloadJson, accessToken, correlationId);

            if (httpResponse.IsSuccessStatusCode)
            {
                CreateBatchResponseModel? createBatchResponse = JsonConvert.DeserializeObject<CreateBatchResponseModel>(await httpResponse.Content.ReadAsStringAsync());
                _logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "New batch for {BatchType} created in FSS. Batch ID is {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, createBatchResponse!.BatchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                return createBatchResponse.BatchId;
            }

            _logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Request to create batch for {BatchType} in FSS failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
            throw new FulfilmentException(EventIds.CreateBatchFailed.ToEventId());
        }

        public async Task<string> CreateBatch(Batch batchType, FormattedWeekNumber weekNumber)
        {
            _logger.LogInformation(EventIds.CreateBatchStarted.ToEventId(), "Request to create batch for {BatchType} in FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchType, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            var uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch";
            var accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            var createBatchRequest = CreateBatchRequestModel(batchType, weekNumber);
            var payloadJson = JsonConvert.SerializeObject(createBatchRequest);
            var httpResponse = await _fssApiClient.CreateBatchAsync(uri, payloadJson, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                var createBatchResponse = JsonConvert.DeserializeObject<CreateBatchResponseModel>(await httpResponse.Content.ReadAsStringAsync());
                _logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "New batch for {BatchType} created in FSS. Batch ID is {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, createBatchResponse!.BatchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return createBatchResponse.BatchId;
            }

            _logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Request to create batch for {BatchType} in FSS failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
            throw new FulfilmentException(EventIds.CreateBatchFailed.ToEventId());
        }

        public async Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength, string mimeType, Batch batchType, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.AddFileToBatchRequestStarted.ToEventId(), "Adding file {FileName} in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            AddFileToBatchRequestModel addFileRequest = CreateAddFileRequestModel(fileName, batchType);
            string payloadJson = JsonConvert.SerializeObject(addFileRequest);
            HttpResponseMessage httpResponseMessage = await _fssApiClient.AddFileToBatchAsync(uri, payloadJson, accessToken, fileLength, mimeType, correlationId);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.AddFileToBatchRequestCompleted.ToEventId(), "File {FileName} is added in batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponseMessage.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                return httpResponseMessage.StatusCode == HttpStatusCode.Created;
            }

            _logger.LogError(EventIds.AddFileToBatchRequestFailed.ToEventId(), "Request to add file {FileName} to batch with BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponseMessage.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
            throw new FulfilmentException(EventIds.AddFileToBatchRequestFailed.ToEventId());
        }

        public async Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.UploadFileBlockStarted.ToEventId(), "Uploading of file blocks of {FileName} for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileInfo.Name, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

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

                ParallelBlockUploadTasks.Add(UploadFileBlock(uploadFileBlockRequestModel, correlationId));
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

            _logger.LogInformation(EventIds.UploadFileBlockCompleted.ToEventId(), "Uploading of file blocks of {FileName} for BatchID - {BatchID} completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileInfo.Name, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));
            return blockIdList;
        }

        public async Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.WriteBlockToFileStarted.ToEventId(), "Writing blocks in file {FileName} for batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            WriteBlockFileRequestModel writeBlockfileRequestModel = new()
            {
                BlockIds = blockIds
            };

            string payloadJson = JsonConvert.SerializeObject(writeBlockfileRequestModel);
            HttpResponseMessage httpResponse = await _fssApiClient.WriteBlockInFileAsync(uri, payloadJson, accessToken, "application/octet-stream", correlationId);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.WriteBlockToFileCompleted.ToEventId(), "File blocks written in file {FileName} for batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                return true;
            }

            _logger.LogError(EventIds.WriteBlockToFileFailed.ToEventId(), "Request to write blocks in file {FileName} failed for batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
            throw new FulfilmentException(EventIds.WriteBlockToFileFailed.ToEventId());
        }

        public async Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames, Batch batchType, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.CommitBatchStarted.ToEventId(), "Batch commit for {BatchType} with BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchType, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            List<FileDetail> fileDetails = _fileSystemHelper.GetFileMD5(fileNames);
            BatchCommitRequestModel batchCommitRequestModel = new()
            {
                FileDetails = fileDetails
            };

            string payloadJson = JsonConvert.SerializeObject(batchCommitRequestModel.FileDetails);
            HttpResponseMessage httpResponse = await _fssApiClient.CommitBatchAsync(uri, payloadJson, accessToken, correlationId);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.CommitBatchCompleted.ToEventId(), "Batch {BatchType} with BatchID - {BatchID} committed in FSS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode, CommonHelper.GetCorrelationId(correlationId));
                return true;
            }

            _logger.LogError(EventIds.CommitBatchFailed.ToEventId(), "Batch commit for {BatchType} failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchType, batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode, CommonHelper.GetCorrelationId(correlationId));
            throw new FulfilmentException(EventIds.CommitBatchFailed.ToEventId());
        }

        public async Task<IEnumerable<BatchFile>> GetAioInfoFolderFilesAsync(string batchId, string correlationId)
        {
            IEnumerable<BatchFile>? fileDetails = null;
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch?$filter=$batch(Content) eq '{_fssApiConfiguration.Value.Content}' and $batch(Product Type) eq '{_fssApiConfiguration.Value.ProductType}'";

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            HttpResponseMessage httpResponseMessage = await _fssApiClient.GetAncillaryFileDetailsAsync(uri, accessToken);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponseAsync(httpResponseMessage);
                if (searchBatchResponse.Count > 0 && searchBatchResponse.Entries.Count > 0)
                {
                    GetBatchResponseModel? batchResult = searchBatchResponse.Entries.OrderByDescending(j => j.BatchPublishedDate).FirstOrDefault();
                    fileDetails = batchResult!.Files.Select(x => new BatchFile
                    {
                        Filename = x.Filename,
                        FileSize = x.FileSize,
                        Links = new Links
                        {
                            Get = new Link
                            {
                                Href = x.Links.Get.Href
                            }
                        }
                    });
                }
                else
                {
                    _logger.LogError(EventIds.GetAioInfoFolderFilesNotFound.ToEventId(), "Error in file share service, aio info folder files not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.GetAioInfoFolderFilesNotFound.ToEventId());
                }
                _logger.LogInformation(EventIds.GetAioInfoFolderFilesOkResponse.ToEventId(), "Successfully searched aio info folder files path for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }
            else
            {
                _logger.LogError(EventIds.GetAioInfoFolderFilesNonOkResponse.ToEventId(), "Error in file share service while searching aio info folder files with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponseMessage!.RequestMessage!.RequestUri, httpResponseMessage.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.GetAioInfoFolderFilesNonOkResponse.ToEventId());
            }
            return fileDetails;
        }

        public async Task<string> SearchReadMeFilePathAsync(string correlationId, string readMeSearchFilter)
        {
            var accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);
            var uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch?$filter={readMeSearchFilter}";

            HttpResponseMessage httpResponse = await _fssApiClient.GetAncillaryFileDetailsAsync(uri, accessToken, correlationId);

            if (httpResponse.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponseAsync(httpResponse);
                if (searchBatchResponse.Entries.Any())
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    if (batchResult!.Files.Count() == 1 && batchResult.Files.Any(x => x.Filename.ToUpper() == _fssApiConfiguration.Value.ReadMeFileName))
                    {
                        return batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                    }

                    _logger.LogError(EventIds.QueryFileShareServiceMultipleFilesFound.ToEventId(), "Error in file share service while searching readme.txt file, multiple files are found for _X-Correlation-ID:{CorrelationId}", correlationId);
                    throw new FulfilmentException(EventIds.QueryFileShareServiceMultipleFilesFound.ToEventId());
                }

                _logger.LogError(EventIds.ReadMeTextFileNotFound.ToEventId(), "Error in file share service while searching readme.txt file not found for _X-Correlation-ID:{CorrelationId}", correlationId);
                throw new FulfilmentException(EventIds.ReadMeTextFileNotFound.ToEventId());
            }

            _logger.LogError(EventIds.QueryFileShareServiceReadMeFileNonOkResponse.ToEventId(), "Error in file share service while searching readme.txt file with uri {RequestUri} responded with {StatusCode} for _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage?.RequestUri, httpResponse.StatusCode, correlationId);
            throw new FulfilmentException(EventIds.QueryFileShareServiceReadMeFileNonOkResponse.ToEventId());
        }

        public async Task<bool> DownloadReadMeFileAsync(string readMeFilePath, string exchangeSetRootPath, string correlationId)
        {
            var accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);
            string fileName = _fssApiConfiguration.Value.ReadMeFileName;
            string filePath = Path.Combine(exchangeSetRootPath, fileName);

            HttpResponseMessage httpReadMeFileResponse = await _fssApiClient.DownloadFile(readMeFilePath.TrimStart('/'), accessToken);

            var requestUri = new Uri(httpReadMeFileResponse.RequestMessage?.RequestUri?.ToString()).GetLeftPart(UriPartial.Path);

            if (httpReadMeFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpReadMeFileResponse.Headers.Server.ToString().Split('/').First();
                await using Stream stream = await httpReadMeFileResponse.Content.ReadAsStreamAsync();
                if (serverValue == ServerHeaderValue)
                {
                    _logger.LogInformation(EventIds.DownloadReadmeFile307RedirectResponse.ToEventId(), "File share service download readme.txt file redirected with uri:{requestUri} responded with 307 code for _X-Correlation-ID:{CorrelationId}", requestUri, correlationId);
                }
                return _fileSystemHelper.DownloadReadmeFile(filePath, stream);
            }

            _logger.LogError(EventIds.DownloadReadMeFileNonOkResponse.ToEventId(), "Error in file share service while downloading readme.txt file with uri:{requestUri} responded with {StatusCode} and _X-Correlation-ID:{CorrelationId}", requestUri, httpReadMeFileResponse.StatusCode, correlationId);
            throw new FulfilmentException(EventIds.DownloadReadMeFileNonOkResponse.ToEventId());
        }

        //Private Methods
        [ExcludeFromCodeCoverage]
        private CreateBatchRequestModel CreateBatchRequestModel(Batch batchType, FormattedWeekNumber weekNumber)
        {
            var createBatchRequest = batchType.IsAio() ? AddBatchAttributesForAio(weekNumber) : AddBatchAttributesForPos(weekNumber);

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

                case Batch.AioBaseCDZipIsoSha1Batch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "AIO"));
                    break;

                case Batch.AioUpdateZipBatch:
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Update"));
                    createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    break;

                default:
                    break;
            };
            return createBatchRequest;
        }

        [ExcludeFromCodeCoverage]
        private CreateBatchRequestModel AddBatchAttributesForPos(FormattedWeekNumber weekNumber)
        {
            var createBatchRequest = new CreateBatchRequestModel
            {
                BusinessUnit = _fssApiConfiguration.Value.BusinessUnit,
                ExpiryDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl
                {
                    ReadUsers = string.IsNullOrEmpty(_fssApiConfiguration.Value.PosReadUsers) ? new List<string>() : _fssApiConfiguration.Value.PosReadUsers.Split(","),
                    ReadGroups = string.IsNullOrEmpty(_fssApiConfiguration.Value.PosReadGroups) ? new List<string>() : _fssApiConfiguration.Value.PosReadGroups.Split(","),
                },
                Attributes =
                [
                    new("Product Type", "AVCS"),
                    new("Week Number", weekNumber.Week),
                    new("Year", weekNumber.Year),
                    new("Year / Week", weekNumber.YearWeek)
                ]
            };
            return createBatchRequest;
        }

        private CreateBatchRequestModel AddBatchAttributesForAio(FormattedWeekNumber weekNumber)
        {
            var aioJobConfigurationEntities = _azureTableStorageHelper.GetAioJobConfiguration();

            (var businessUnit, var readUsers, var readGroups) = (
                aioJobConfigurationEntities?.BusinessUnit ?? _fssApiConfiguration.Value.AioBusinessUnit,
                aioJobConfigurationEntities?.ReadUsers ?? _fssApiConfiguration.Value.AioReadUsers,
                aioJobConfigurationEntities?.ReadGroups ?? _fssApiConfiguration.Value.AioReadGroups
            );

            var createBatchRequest = new CreateBatchRequestModel
            {
                BusinessUnit = businessUnit,
                ExpiryDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl
                {
                    ReadUsers = string.IsNullOrEmpty(readUsers) ? new List<string>() : readUsers.Split(","),
                    ReadGroups = string.IsNullOrEmpty(readGroups) ? new List<string>() : readGroups.Split(","),
                },
                Attributes =
                [
                    new("Product Type", "AIO"),
                    new("Week Number", weekNumber.Week),
                    new("Year", weekNumber.Year),
                    new("Year / Week", weekNumber.YearWeek)
                ]
            };
            return createBatchRequest;
        }

        [ExcludeFromCodeCoverage]
        private CreateBatchRequestModel CreateBatchRequestModelForBess(Batch batchType, ConfigQueueMessage configQueueMessage)
        {
            List<KeyValuePair<string, string>> batchAttributes = new();

            foreach (Tag tag in configQueueMessage.Tags)
            {
                batchAttributes.Add(new KeyValuePair<string, string>(tag.Key, tag.Value));
            }

            CreateBatchRequestModel createBatchRequest = new()
            {
                BusinessUnit = _fssApiConfiguration.Value.BessBusinessUnit,
                ExpiryDate = DateTime.UtcNow.AddDays(configQueueMessage.BatchExpiryInDays).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl
                {
                    ReadUsers = configQueueMessage.AllowedUsers,
                    ReadGroups = configQueueMessage.AllowedUserGroups,
                },
                Attributes = batchAttributes
            };

            //This batch attribute is added for fss stub.
            if (bool.Parse(_configuration["IsFTRunning"]))
            {
                createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Batch Type", batchType.ToString()));
            }
            return createBatchRequest;
        }

        [ExcludeFromCodeCoverage]
        private static AddFileToBatchRequestModel CreateAddFileRequestModel(string fileName, Batch batchType)
        {
            var addFileToBatchRequestModel = new AddFileToBatchRequestModel
            {
                Attributes = new List<KeyValuePair<string, string>>
                {
                    new("Product Type", batchType.IsAio() ? "AIO" : "AVCS"),
                    new("File Name", fileName)
                }
            };
            return addFileToBatchRequestModel;
        }

        [ExcludeFromCodeCoverage]
        private async Task UploadFileBlock(UploadFileBlockRequestModel uploadBlockMetaData, string? correlationId = null)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{uploadBlockMetaData.BatchId}/files/{uploadBlockMetaData.FileName}/{uploadBlockMetaData.BlockId}";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId, correlationId);

            byte[] blockBytes = _fileSystemHelper.GetFileInBytes(uploadBlockMetaData);
            byte[]? blockMd5Hash = CommonHelper.CalculateMD5(blockBytes);
            HttpResponseMessage httpResponse;
            httpResponse = await _fssApiClient.UploadFileBlockAsync(uri, blockBytes, blockMd5Hash, accessToken, "application/octet-stream", correlationId);

            if (httpResponse.IsSuccessStatusCode)
            {
                await Task.CompletedTask;
            }
            else
            {
                _logger.LogError(EventIds.UploadFileBlockFailed.ToEventId(), "Request to upload block {BlockID} of {FileName} failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", uploadBlockMetaData.BlockId, uploadBlockMetaData.FileName, uploadBlockMetaData.BatchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.GetCorrelationId(correlationId));
                throw new FulfilmentException(EventIds.UploadFileBlockFailed.ToEventId());
            }
        }

        [ExcludeFromCodeCoverage]
        private static async Task<SearchBatchResponse> SearchBatchResponseAsync(HttpResponseMessage httpResponse)
        {
            string body = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchBatchResponse>(body);
        }
    }
}
