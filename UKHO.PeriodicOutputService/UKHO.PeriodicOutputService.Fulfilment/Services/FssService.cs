using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.Request;
using System.Globalization;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

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
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            var uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch";

            CreateBatchRequestModel createBatchRequest = CreateBatchRequestModel();

            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            var httpResponse = await _fssApiClient.CreateBatchAsync(uri, payloadJson, accessToken);

            CreateBatchResponseModel createBatchResponse = JsonConvert.DeserializeObject<CreateBatchResponseModel>(await httpResponse.Content.ReadAsStringAsync());

            return createBatchResponse.BatchId;
        }

        public async Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            AddFileToBatchRequestModel addFileRequest = CreateAddFileRequestModel();
            string payloadJson = JsonConvert.SerializeObject(addFileRequest);

            HttpResponseMessage httpResponseMessage = await _fssApiClient.AddFileToBatchAsync(uri, payloadJson, accessToken, fileLength, "application/octet-stream");

            Console.WriteLine("------------Status Code is - " + httpResponseMessage.StatusCode.ToString());

            if (httpResponseMessage.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }

            return false;
        }

        public async Task<List<string>> UploadBlocks(string batchId, FileInfo fileInfo)
        {
            int fileBlockSize = _fssApiConfiguration.Value.BlockSizeInMultipleOfKBs;

            long blockSizeInMultipleOfKBs = fileBlockSize <= 0 || fileBlockSize > 4096
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
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

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
            return true;
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId)
        {
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

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
                    break;

                _logger.LogInformation(EventIds.BatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchId} completed. Batch Status is {BatchStatus} at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                await Task.Delay(int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTime));

            }
            return batchStatus;
        }

        public async Task<GetBatchResponseModel> GetBatchDetails(string batchId)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";

            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage batchDetailResponse = await _fssApiClient.GetBatchDetailsAsync(uri, accessToken);

            if (!batchDetailResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Request to get batch details for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return null;
            }

            _logger.LogInformation(EventIds.GetBatchDetailRequestCompleted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return JsonConvert.DeserializeObject<GetBatchResponseModel>(await batchDetailResponse.Content.ReadAsStringAsync());
        }

        public async Task<Stream> DownloadFile(string downloadPath, string fileName, string fileLink)
        {
            _logger.LogInformation(EventIds.DownloadFileStarted.ToEventId(), "Downloading file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string filePath = string.Empty;
            string fileUri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(fileUri, accessToken);

            if (!fileDownloadResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), fileDownloadResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return null;
            }

            _logger.LogInformation(EventIds.DownloadFileCompleted.ToEventId(), "Downloading file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return await fileDownloadResponse.Content.ReadAsStreamAsync();
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
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{uploadBlockMetaData.BatchId}/files/{uploadBlockMetaData.FileName}/{uploadBlockMetaData.BlockId}";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NzU5NTY2LCJuYmYiOjE2NTg3NTk1NjYsImV4cCI6MTY1ODc2MzU2MCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWpzdXdpemlUdWV6S3h4TTVCUVZsQytHeEhDSmpjN3p6OC8zSTRNbW1KMUV2QnRxcDcvUXp1aUM0MmdoeEFJNTFCaG5wUkVsZHdlQjFmQ3NqSk9FdXJ1U3ViVjFpWndrZXZTd2lOcXFPZHF5TGpXQXJaNFRCT1ltQTJlVGlCenZZUmZ0MW5sYU5ZV0hsd3ZyRU8vS2NGeTIwY0E3THZXT1BWVU82b1RyRENBND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoibGg2OGVadkRUazZ0MnhNY2JLaVNBQSIsInZlciI6IjEuMCJ9.cLtnUUHM2WGbr2aoaFIMfpqtDp0T95DHP5rN3eXG9biLpdQQVAI1naS_e8Bufthzk7WF__4TVOIoBODoJj-KpmFt5sV8iR2ckP5igpcmblvUpcZRgvBRSQ2MYzp0kxorEH-htqm3qnYbA629qBAihgKcM59JS_BAMeG6XEp6n5l2c0vP0536MRgAvyVLM1w_PIn-9xUwpiDLJCR9b0IdQzfl9kNQJNrLoPSfH5neZC0el41xj4ukYyjh5OB1QlNOpMRuCPQl9S6vRgdgDMnwxmSl01DNRX7CFLcZuejFmPpqYnpq3ABjtmCteD39zvCjhXmy22uAt3oJu_i0588FUQ";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            byte[] blockBytes = _fileSystemHelper.GetFileInBytes(uploadBlockMetaData);
            var blockMd5Hash = CommonHelper.CalculateMD5(blockBytes);
            HttpResponseMessage httpResponse;
            httpResponse = await _fssApiClient.UploadFileBlockAsync(uri, blockBytes, blockMd5Hash, accessToken, "application/octet-stream");

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.UploadFileBlockFailed.ToEventId(), "Error in uploading file blocks with uri {RequestUri} responded with {StatusCode} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, uploadBlockMetaData.BlockId, uploadBlockMetaData.FileName, uploadBlockMetaData.BatchId, CommonHelper.CorrelationID.ToString());
            }
        }
    }
}
