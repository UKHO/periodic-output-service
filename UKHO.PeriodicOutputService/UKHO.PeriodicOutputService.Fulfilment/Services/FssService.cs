using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.FileShareService.Response;
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

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId)
        {
            _logger.LogInformation(EventIds.FssBatchStatusPollingStarted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
           
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

            FssBatchStatus[] pollBatchStatus = { FssBatchStatus.CommitInProgress, FssBatchStatus.Incomplete };

            while (pollBatchStatus.Contains(batchStatus) &&
                        DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime)))
            {
                _logger.LogInformation(EventIds.GetBatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken);

                if (batchStatusResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation(EventIds.GetBatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);

                    FssBatchStatusResponseModel fssBatchStatusResponseModel = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                    Enum.TryParse(fssBatchStatusResponseModel?.Status, false, out batchStatus);

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
                _logger.LogError(EventIds.FssBatchStatusPollingTimedOut.ToEventId(), "Fss batch status polling timed out for BatchID - {BatchID} failed | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.FssBatchStatusPollingTimedOut.ToEventId());
            }

            _logger.LogInformation(EventIds.FssBatchStatusPollingCompleted.ToEventId(), "Polling to FSS to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return batchStatus;
        }

        public async Task<GetBatchResponseModel> GetBatchDetails(string batchId)
        {
            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchID} from FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

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

        public async Task<Stream> DownloadFile(string fileName, string fileLink)
        {
            _logger.LogInformation(EventIds.DownloadFileStarted.ToEventId(), "Downloading of file {fileName} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(uri, accessToken);

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

        public async Task<string> CreateBatch(string mediaType)
        {
            _logger.LogInformation(EventIds.CreateBatchStarted.ToEventId(), "Request to create batch for {MediaType} in FSS started | {DateTime} | _X-Correlation-ID : {CorrelationId}", mediaType, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string? uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

            CreateBatchRequestModel createBatchRequest = CreateBatchRequestModel(mediaType);
            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);
            HttpResponseMessage? httpResponse = await _fssApiClient.CreateBatchAsync(uri, payloadJson, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                CreateBatchResponseModel createBatchResponse = JsonConvert.DeserializeObject<CreateBatchResponseModel>(await httpResponse.Content.ReadAsStringAsync());
                _logger.LogInformation(EventIds.CreateBatchCompleted.ToEventId(), "New batch for {MediaType} created in FSS. Batch ID is {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", mediaType, createBatchResponse.BatchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                return createBatchResponse.BatchId;
            }
            else
            {
                _logger.LogError(EventIds.CreateBatchFailed.ToEventId(), "Request to create batch for {MediaType} in FSS failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", mediaType, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.CreateBatchFailed.ToEventId());
            }
        }

        public async Task<bool> AddFileToBatch(string batchId, string fileName, long fileLength)
        {
            _logger.LogInformation(EventIds.AddFileToBatchRequestStarted.ToEventId(), "Adding file {FileName} in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}", fileName, batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/files/{fileName}";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

            AddFileToBatchRequestModel addFileRequest = CreateAddFileRequestModel();
            string payloadJson = JsonConvert.SerializeObject(addFileRequest);
            HttpResponseMessage httpResponseMessage = await _fssApiClient.AddFileToBatchAsync(uri, payloadJson, accessToken, fileLength, "application/octet-stream");

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
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

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

        public async Task<bool> CommitBatch(string batchId, IEnumerable<string> fileNames)
        {
            _logger.LogInformation(EventIds.CommitBatchStarted.ToEventId(), "Batch commit for batch with BatchID - {BatchID} started | {DateTime} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID.ToString());

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

            List<FileDetail> fileDetails = _fileSystemHelper.GetFileMD5(fileNames);
            BatchCommitRequestModel batchCommitRequestModel = new()
            {
                FileDetails = fileDetails
            };

            string payloadJson = JsonConvert.SerializeObject(batchCommitRequestModel.FileDetails);
            HttpResponseMessage httpResponse = await _fssApiClient.CommitBatchAsync(uri, payloadJson, accessToken);

            if (httpResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.CommitBatchCompleted.ToEventId(), "Batch with BatchID - {BatchID} committed in FSS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode, CommonHelper.CorrelationID.ToString());
                return true;
            }
            else
            {
                _logger.LogError(EventIds.CommitBatchFailed.ToEventId(), "Batch commit failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), httpResponse.StatusCode, CommonHelper.CorrelationID.ToString());
                throw new FulfilmentException(EventIds.AddFileToBatchRequestFailed.ToEventId());
            }
        }

        //Private Methods
        private CreateBatchRequestModel CreateBatchRequestModel(string mediaType)
        {
            string currentYear = DateTime.UtcNow.Year.ToString();
            string currentWeek = CommonHelper.GetCurrentWeekNumber(DateTime.UtcNow).ToString();

            CreateBatchRequestModel createBatchRequest = new()
            {
                BusinessUnit = "AVCSData",
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Base"),
                    new KeyValuePair<string, string>("Media Type", mediaType),
                    new KeyValuePair<string, string>("Product Type", "AVCS"),
                    new KeyValuePair<string, string>("S63 Version", "1.2"),
                    new KeyValuePair<string, string>("Week Number", currentWeek),
                    new KeyValuePair<string, string>("Year", currentYear),
                    new KeyValuePair<string, string>("Year / Week", currentYear + " / " + currentWeek)
                },
                ExpiryDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { "public" }
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
                    new KeyValuePair<string, string>("Exchange Set Type", "Base"),
                    new KeyValuePair<string, string>("Media Type", "DVD"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                }
            };
            return addFileToBatchRequestModel;
        }

        private async Task UploadFileBlock(UploadFileBlockRequestModel uploadBlockMetaData)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{uploadBlockMetaData.BatchId}/files/{uploadBlockMetaData.FileName}/{uploadBlockMetaData.BlockId}";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjYwODE1NjY5LCJuYmYiOjE2NjA4MTU2NjksImV4cCI6MTY2MDgyMTA0NCwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQUVrR3BmbVM3RGVjd3c0RHp0M0dha2NXamdvL2VtbkcvbHhmMit0TVBRdHdwSmFYZGRLamx1MGxGM1ZCVHNCd21USTZFV2hmTkFaWk9NcEJseGQvYmNueHliNHhyRnBuNE9ybjRUOUlaTFF3MXBUQk4xdlRXUHA0MkRNWkdiQW5LZ1UwVXBnNDlNcjVoVXpHekFRaEVTR3pjb0hCZ1Q2MXI2eHMyMDk5V1RUUT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjE2My4xMTYuMjA1LjExOCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiTWx4Z1M2OHEwRU9YZUxuWjRsdGhBQSIsInZlciI6IjEuMCJ9.l2BHgsfEZsJzdX3a9yxkxE7eu8szC2H5dg-_Llp7gKSVdfZlLAMTUZE698wT-wNxfjbHOCLiZPtz2_t0XdTT8-PML_34GJvthhbqmvPpZHvo-jv0PiJTaf0tS99QU3UddNNYECrXQwSNj03_bbbhqercrEbGYEEqMum-yImKRkl5RoCnfqZN0HPJ7zt4linAbTUAj1bDFHxkHJriDN3TiwH8gzSD9ieIuB_LwFwg7VVdI9S3EFW1tLKbVA5e2b82F_bCssF1iASJ2xKjYQzr3l8XLLTKp-e33AQWERni4M425oAmoSKyFubrOCV-hw_5FpFiN_YfZRgrXMWwoBjcHA";

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
