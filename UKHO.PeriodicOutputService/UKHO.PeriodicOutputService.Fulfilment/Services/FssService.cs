using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.Request;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FssService : IFssService
    {
        private readonly IOptions<FssApiConfiguration> _fssApiConfiguration;
        private readonly ILogger<FssService> _logger;
        private readonly IFssApiClient _fssApiClient;
        private readonly IAuthFssTokenProvider _authFssTokenProvider;
        private readonly IFileSystemHelper _fileSystemHelper;

        public FssService(ILogger<FssService> logger,
                               IOptions<FssApiConfiguration> fssApiConfiguration,
                               IFssApiClient fssApiClient,
                               IAuthFssTokenProvider authFssTokenProvider,
                               IFileSystemHelper fileSystemHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fssApiConfiguration = fssApiConfiguration ?? throw new ArgumentNullException(nameof(fssApiConfiguration));
            _fssApiClient = fssApiClient ?? throw new ArgumentNullException(nameof(fssApiClient));
            _authFssTokenProvider = authFssTokenProvider ?? throw new ArgumentNullException(nameof(authFssTokenProvider));
            _fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
        }

        public async Task<string> CreateBatch()
        {
            _logger.LogInformation(EventIds.CreateBatchStarted.ToEventId(), "Request to create batch started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string? uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch";

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            CreateBatchRequestModel createBatchRequest = CreateBatchRequestModel();

            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            HttpResponseMessage? httpResponse = await _fssApiClient.CreateBatchAsync(uri, payloadJson, accessToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Request to create batch failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return null;
            }

            CreateBatchResponseModel createBatchResponse = JsonConvert.DeserializeObject<CreateBatchResponseModel>(await httpResponse.Content.ReadAsStringAsync());

            _logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "Request to create batch completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return createBatchResponse.BatchId;
        }

        public async Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength)
        {
            _logger.LogInformation(EventIds.AddFileToBatchRequestStarted.ToEventId(), "Request to add file to batch for BatchID - {BatchId} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            AddFileToBatchRequestModel addFileRequest = CreateAddFileRequestModel();

            string payloadJson = JsonConvert.SerializeObject(addFileRequest);

            HttpResponseMessage httpResponseMessage = await _fssApiClient.AddFileToBatchAsync(uri, payloadJson, accessToken, fileLength, "application/octet-stream");

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.AddFileToBatchRequestFailed.ToEventId(), "Request to add file to batch for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), httpResponseMessage.StatusCode.ToString(), CommonHelper.CorrelationID);
                return false;
            }

            _logger.LogInformation(EventIds.AddFileToBatchRequestCompleted.ToEventId(), "Request to add file to batch for BatchID - {BatchId} completed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), httpResponseMessage.StatusCode.ToString(), CommonHelper.CorrelationID);

            return httpResponseMessage.StatusCode == HttpStatusCode.Created;
        }

        public async Task<List<string>> UploadBlocks(string batchId, IFileInfo fileInfo)
        {
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
            return blockIdList;
        }

        public async Task<bool> WriteBlockFile(string batchId, string fileName, IEnumerable<string> blockIds)
        {
            _logger.LogInformation(EventIds.WriteBlockToFileStarted.ToEventId(), "Request to write blocks file to batch - {BatchId} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            WriteBlockFileRequestModel writeBlockfileRequestModel = new()
            {
                BlockIds = blockIds
            };

            string payloadJson = JsonConvert.SerializeObject(writeBlockfileRequestModel);

            HttpResponseMessage httpResponse = await _fssApiClient.WriteBlockInFileAsync(uri, payloadJson, accessToken, "application/octet-stream");

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.WriteBlockToFileFailed.ToEventId(), "Error in writing Blocks with uri:{RequestUri} responded with {StatusCode} for file:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, fileName, batchId, CommonHelper.CorrelationID.ToString());
                return false;
            }

            _logger.LogInformation(EventIds.WriteBlockToFileCompleted.ToEventId(), "Request to write blocks file to batch - {BatchId} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return true;
        }

        public async Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames)
        {
            _logger.LogInformation(EventIds.CommitBatchStarted.ToEventId(), "Upload Commit Batch started for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, CommonHelper.CorrelationID.ToString());

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            List<FileDetail> fileDetails = _fileSystemHelper.GetFileMD5(fileNames);
            BatchCommitRequestModel batchCommitRequestModel = new()
            {
                FileDetails = fileDetails
            };

            string payloadJson = JsonConvert.SerializeObject(batchCommitRequestModel.FileDetails);

            HttpResponseMessage httpResponse = await _fssApiClient.CommitBatchAsync(uri, payloadJson, accessToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.CommitBatchFailed.ToEventId(), "Error in Upload Commit Batch with uri:{RequestUri} responded with {StatusCode} BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, CommonHelper.CorrelationID.ToString());
                return false;
            }

            _logger.LogInformation(EventIds.CommitBatchCompleted.ToEventId(), "Upload Commit Batch completed for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, CommonHelper.CorrelationID.ToString());

            return true;
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId)
        {
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestCompleted.ToEventId(), "Getting access token to call FSS Batch Status endpoint completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime)))
            {
                _logger.LogInformation(EventIds.BatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchId} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken);

                if (!batchStatusResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(EventIds.BatchStatusRequestFailed.ToEventId(), "Request to get batch status for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                    break;
                }
                FssBatchStatusResponseModel responseObj = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());

                Enum.TryParse(responseObj?.Status, false, out batchStatus);

                if (batchStatus == FssBatchStatus.Committed)
                {
                    break;
                }

                _logger.LogInformation(EventIds.BatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchId} completed. Batch Status is {BatchStatus} at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                await Task.Delay(int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTime));

            }
            return batchStatus;
        }

        public async Task<GetBatchResponseModel> GetBatchDetails(string batchId)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";

            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage batchDetailResponse = await _fssApiClient.GetBatchDetailsAsync(uri, accessToken);

            if (!batchDetailResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Request to get batch details for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return null;
            }

            _logger.LogInformation(EventIds.GetBatchDetailRequestCompleted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return JsonConvert.DeserializeObject<GetBatchResponseModel>(await batchDetailResponse.Content.ReadAsStringAsync());
        }

        //Private Methods
        private CreateBatchRequestModel CreateBatchRequestModel()
        {
            CreateBatchRequestModel createBatchRequest = new()
            {
                BusinessUnit = "AVCSData",
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Full AVCS"),
                    new KeyValuePair<string, string>("Media Type", "DVD"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { "POS" }
                }
            };

            return createBatchRequest;
        }

        private AddFileToBatchRequestModel CreateAddFileRequestModel()
        {
            AddFileToBatchRequestModel addFileToBatchRequestModel = new()
            {
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Full AVCS"),
                    new KeyValuePair<string, string>("Media Type", "DVD"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                }
            };

            return addFileToBatchRequestModel;
        }

        private async Task UploadFileBlock(UploadFileBlockRequestModel uploadBlockMetaData)
        {
            _logger.LogInformation(EventIds.UploadFileBlockStarted.ToEventId(), "Uploading file blocks for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", uploadBlockMetaData.BlockId, uploadBlockMetaData.FileName, uploadBlockMetaData.BatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{uploadBlockMetaData.BatchId}/files/{uploadBlockMetaData.FileName}/{uploadBlockMetaData.BlockId}";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            ////string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5NTMxODY2LCJuYmYiOjE2NTk1MzE4NjYsImV4cCI6MTY1OTUzNjY2MiwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXA5WENhV1JvbDl5N1VRVEFjNnpESldtcGNzSHE1eUVEMytwTEoxU0JtVDZZdVZCK3ZXNHVGMEg5RUVuMVBDTTlLNEJRUncwc002STZNWWtFYW1vWW9QVis4dEgra2RUK0oyTTFzelhDQ3U1MlRhWkF6cTVOTDZkTnZjNXBWUTUxemwvZFZFclFzdmJYL0t5QmNuclk5ZmxGckNNa2E4OWtCNWhiS2FEaW9iMD0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJoYXJzaGFsMTE4NjlAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiU2h1a2xhIiwiZ2l2ZW5fbmFtZSI6IkhhcnNoYWwiLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjE1OS45Ny4yNSIsIm5hbWUiOiJIYXJzaGFsIFNodWtsYSIsIm9pZCI6IjQ5MTg2NGY1LWIyYzYtNGY1MC05NDUxLThlMzdjMWQ4YWI0NCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSXMuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiblVncHBudDJmc0hOU3Jtb2ZDUENSaWNLcTAtcE9PekRDRnhyNzEzVkFnbyIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiaGFyc2hhbDExODY5QG1hc3Rlay5jb20iLCJ1dGkiOiJ3TlRxcnNyM1ZFT0hXZHFDc0dWNkFBIiwidmVyIjoiMS4wIn0.WQL8J709sF-f7AzMmg0oYNTPMccTJNUaXBu_jC-WKuJEmcxj5qmgsbWxauMxwYncIpYoQ5EekJoVweZYas6xw50RMtKZNsJ5nI48kb2NiPuXHPkQ7JSq8qDtfFgn2ADhFxZ2uGWDHLefcYbChjDSov7_6ELXxR-eqY94F-LFDAwzyz4ds61dvr7ZHUnwWK7xHlcHRsz28j392xyPZ0zezIG0qf7zhI0L3-QLJmbHDkwGDyL2ZP9gEaBkctrCx17GcToJAcuTxpH2cDmPfivNoknGKCbaAdM3isVY3cXEpln2zIvuuLPYY7o4m5UwvPYua4akRMkqabKBBEWuIzS8yw";
            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            byte[] blockBytes = _fileSystemHelper.GetFileInBytes(uploadBlockMetaData);
            byte[]? blockMd5Hash = CommonHelper.CalculateMD5(blockBytes);
            HttpResponseMessage httpResponse;
            httpResponse = await _fssApiClient.UploadFileBlockAsync(uri, blockBytes, blockMd5Hash, accessToken, "application/octet-stream");

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.UploadFileBlockFailed.ToEventId(), "Error in uploading file blocks with uri {RequestUri} responded with {StatusCode} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, uploadBlockMetaData.BlockId, uploadBlockMetaData.FileName, uploadBlockMetaData.BatchId, CommonHelper.CorrelationID.ToString());
                throw new HttpRequestException();
            }

            _logger.LogInformation(EventIds.UploadFileBlockCompleted.ToEventId(), "Uploading file blocks for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", uploadBlockMetaData.BlockId, uploadBlockMetaData.FileName, uploadBlockMetaData.BatchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            await Task.CompletedTask;
        }
    }
}
