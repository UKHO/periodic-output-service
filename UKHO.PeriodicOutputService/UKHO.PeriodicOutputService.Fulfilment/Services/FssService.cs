using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
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

        public FssService(ILogger<FssService> logger,
                               IOptions<FssApiConfiguration> fssApiConfiguration,
                               IFssApiClient fssApiClient,
                               IAuthFssTokenProvider authFssTokenProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fssApiConfiguration = fssApiConfiguration ?? throw new ArgumentNullException(nameof(fssApiConfiguration));
            _fssApiClient = fssApiClient ?? throw new ArgumentNullException(nameof(fssApiClient));
            _authFssTokenProvider = authFssTokenProvider ?? throw new ArgumentNullException(nameof(authFssTokenProvider));
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId)
        {
            _logger.LogInformation(EventIds.FssBatchStatusPollingStarted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5OTczMTQ2LCJuYmYiOjE2NTk5NzMxNDYsImV4cCI6MTY1OTk3ODA4MSwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQU0wTW9DOWR5UFlONjJ6bkI0NnBzc2RvRFFrVWxYYWtYN3lSV1RVMWd3cFdaL2U4N1p2bWFyYWsxTTBOZWcydHFPZjlGbTNrUlByRmVqWTdrbWk3NXRneUttK1R5U1hzYVJETzNaQUhmRGM0PSIsImFtciI6WyJwd2QiXSwiYXBwaWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTcuMjM5IiwibmFtZSI6IlN1bW9kIEVkYXRoYXJhIiwib2lkIjoiMDY5NjYxMTgtOTkyMC00N2VmLTk2MDQtMTRiYzkzY2M3ZGYwIiwicmgiOiIwLkFWTUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FDay4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiJteTZVcDctYnFnRm4zWmYzSzM4MWdwdjBhMmM0MFRKM3dJLW5FUnhudEdJIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJzdW1vZDEzOTQzQG1hc3Rlay5jb20iLCJ1dGkiOiJmdmllOHVIemJrYS1NQ2ZSTTVzNkFBIiwidmVyIjoiMS4wIn0.llX0hRQBtIF4mBBywYd_17RWOObKMY42y5it88KaaUw4dmx-INmmHtsTQF_u0wuLyE3w0TIDnEV6rjDiKeWJ5D8dUNx0S-mwZWaMGWpE_hn2R_X7iQk4bpIi2RP2BZjSkjYvY04AR0cKC-VK0koKLilIR4uaJ6hHKM2cCnom9lkfIy4B-0PuRyA2m1UkUCWITdpLTHgS6JRzLLE4_V0xxsbKs2hdLdAgXRz0BaFPmcDtIzLXRgiUBCP8mIuSMO4TUK6Exinu5XIhbjgYM3tl8ea1Tb8qDyfvZIrxxNJENZfCzwymr42WamPcUHmAPZRINVD8Xd2KPjVkoCBKcGTYjg";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime)))
            {
                _logger.LogInformation(EventIds.GetBatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken);
                if (batchStatusResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation(EventIds.GetBatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);

                    FssBatchStatusResponseModel fssBatchStatusResponseModel = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                    Enum.TryParse(fssBatchStatusResponseModel?.Status, false, out batchStatus);
                    if (batchStatus == FssBatchStatus.Committed)
                    {
                        _logger.LogInformation(EventIds.FssBatchStatusPollingStopped.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} stopped | Batch Status is {BatchStatus} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                        break;
                    }
                    await Task.Delay(int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTime));
                }
                else
                {
                    _logger.LogError(EventIds.GetBatchStatusRequestFailed.ToEventId(), "Request to get batch status for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.GetBatchStatusRequestFailed.ToEventId());
                }
            }
            _logger.LogInformation(EventIds.FssBatchStatusPollingCompleted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return batchStatus;
        }

        public async Task<GetBatchResponseModel> GetBatchDetails(string batchId)
        {
            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchID} from FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5OTczMTQ2LCJuYmYiOjE2NTk5NzMxNDYsImV4cCI6MTY1OTk3ODA4MSwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQU0wTW9DOWR5UFlONjJ6bkI0NnBzc2RvRFFrVWxYYWtYN3lSV1RVMWd3cFdaL2U4N1p2bWFyYWsxTTBOZWcydHFPZjlGbTNrUlByRmVqWTdrbWk3NXRneUttK1R5U1hzYVJETzNaQUhmRGM0PSIsImFtciI6WyJwd2QiXSwiYXBwaWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTcuMjM5IiwibmFtZSI6IlN1bW9kIEVkYXRoYXJhIiwib2lkIjoiMDY5NjYxMTgtOTkyMC00N2VmLTk2MDQtMTRiYzkzY2M3ZGYwIiwicmgiOiIwLkFWTUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FDay4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiJteTZVcDctYnFnRm4zWmYzSzM4MWdwdjBhMmM0MFRKM3dJLW5FUnhudEdJIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJzdW1vZDEzOTQzQG1hc3Rlay5jb20iLCJ1dGkiOiJmdmllOHVIemJrYS1NQ2ZSTTVzNkFBIiwidmVyIjoiMS4wIn0.llX0hRQBtIF4mBBywYd_17RWOObKMY42y5it88KaaUw4dmx-INmmHtsTQF_u0wuLyE3w0TIDnEV6rjDiKeWJ5D8dUNx0S-mwZWaMGWpE_hn2R_X7iQk4bpIi2RP2BZjSkjYvY04AR0cKC-VK0koKLilIR4uaJ6hHKM2cCnom9lkfIy4B-0PuRyA2m1UkUCWITdpLTHgS6JRzLLE4_V0xxsbKs2hdLdAgXRz0BaFPmcDtIzLXRgiUBCP8mIuSMO4TUK6Exinu5XIhbjgYM3tl8ea1Tb8qDyfvZIrxxNJENZfCzwymr42WamPcUHmAPZRINVD8Xd2KPjVkoCBKcGTYjg";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

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

        public async Task<Stream> DownloadFile(string downloadPath, string fileName, string fileLink)
        {
            _logger.LogInformation(EventIds.DownloadFileStarted.ToEventId(), "Downloading of file {fileName} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string fileUri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU5OTczMTQ2LCJuYmYiOjE2NTk5NzMxNDYsImV4cCI6MTY1OTk3ODA4MSwiYWNyIjoiMSIsImFpbyI6IkFWUUFxLzhUQUFBQU0wTW9DOWR5UFlONjJ6bkI0NnBzc2RvRFFrVWxYYWtYN3lSV1RVMWd3cFdaL2U4N1p2bWFyYWsxTTBOZWcydHFPZjlGbTNrUlByRmVqWTdrbWk3NXRneUttK1R5U1hzYVJETzNaQUhmRGM0PSIsImFtciI6WyJwd2QiXSwiYXBwaWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJhcHBpZGFjciI6IjAiLCJlbWFpbCI6InN1bW9kMTM5NDNAbWFzdGVrLmNvbSIsImZhbWlseV9uYW1lIjoiRWRhdGhyYSIsImdpdmVuX25hbWUiOiJTdW1vZCIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2FkZDFjNTAwLWE2ZDctNGRiZC1iODkwLTdmOGNiNmY3ZDg2MS8iLCJpcGFkZHIiOiIxLjE4Ni4xMTcuMjM5IiwibmFtZSI6IlN1bW9kIEVkYXRoYXJhIiwib2lkIjoiMDY5NjYxMTgtOTkyMC00N2VmLTk2MDQtMTRiYzkzY2M3ZGYwIiwicmgiOiIwLkFWTUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FDay4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiJteTZVcDctYnFnRm4zWmYzSzM4MWdwdjBhMmM0MFRKM3dJLW5FUnhudEdJIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJzdW1vZDEzOTQzQG1hc3Rlay5jb20iLCJ1dGkiOiJmdmllOHVIemJrYS1NQ2ZSTTVzNkFBIiwidmVyIjoiMS4wIn0.llX0hRQBtIF4mBBywYd_17RWOObKMY42y5it88KaaUw4dmx-INmmHtsTQF_u0wuLyE3w0TIDnEV6rjDiKeWJ5D8dUNx0S-mwZWaMGWpE_hn2R_X7iQk4bpIi2RP2BZjSkjYvY04AR0cKC-VK0koKLilIR4uaJ6hHKM2cCnom9lkfIy4B-0PuRyA2m1UkUCWITdpLTHgS6JRzLLE4_V0xxsbKs2hdLdAgXRz0BaFPmcDtIzLXRgiUBCP8mIuSMO4TUK6Exinu5XIhbjgYM3tl8ea1Tb8qDyfvZIrxxNJENZfCzwymr42WamPcUHmAPZRINVD8Xd2KPjVkoCBKcGTYjg";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(fileUri, accessToken);

            if (fileDownloadResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.DownloadFileCompleted.ToEventId(), "Downloading of file {fileName} completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), fileDownloadResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return await fileDownloadResponse.Content.ReadAsStreamAsync();
            }
            else
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading of file {fileName} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), fileDownloadResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.DownloadFileFailed.ToEventId());
            }
        }
    }
}
