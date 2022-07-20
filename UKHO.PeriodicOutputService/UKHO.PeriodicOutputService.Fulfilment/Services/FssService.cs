using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;

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
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4MzI4MDY1LCJuYmYiOjE2NTgzMjgwNjUsImV4cCI6MTY1ODMzMjA2MywiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQVVhYk94Y0JGLzZPeWdKdGV6byt1YnNJUnI3UlhWanQxcEQxMDhoRUdmVkpObWFMclJ0MzV1YU9oU0oyY1JCV2ExWlZDOTB4YXk2Rnp6SUFCNVpiSzIvZ01qc29ENVYzUUlqaUxEWGZBV1hoWGVDM25sTzZOeCtkWVhYS3BKelhnQzIwTFQrVFFLcDlPcUlCeTlsbjh4d09EZWc3UC81OGpWOUNDVE5OZXVKYz0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoieTh1YmppV1VFMDJuclEyZF9DUTRBQSIsInZlciI6IjEuMCJ9.HmFlNP6R62JiHXIBg6exbjGhopg3ujRkcEnle24399-aVlsWDCJ-6XA53sX8jpwVYFLg4OTa4Q8864eFvRywbZ_jnN2XAjzT2jK_BWLO2zDUrcbzgVDi1pP9P-hEox0fsbTgT-cnOX7Km7iiO4nRlMhcfwulOmnVnfeezbDJlUU9_ni_rxQRXIwyCVxr2TNf4aYFEfDiPm36iPk66jmGasjZXVCA2xFw7qU9GgHWX8Hos4AblFFmcys0El50q6PiI3e2uXSw-4Tb5aU_eadjDL5aKkcuDnj9aOgmh_YzamZCWaMzO7cg43H_Ni2HUJcoHaWE5WeJS7DlXyBmdytG7Q";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestCompleted.ToEventId(), "Getting access token to call FSS Batch Status endpoint completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime)))
            {
                await Task.Delay(int.Parse(_fssApiConfiguration.Value.BatchStatusPollingDelayTime));

                _logger.LogInformation(EventIds.BatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchId} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken);

                if (!batchStatusResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(EventIds.BatchStatusRequestFailed.ToEventId(), "Request to get batch status for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, batchStatusResponse.StatusCode.ToString(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    break;
                }
                FssBatchStatusResponseModel responseObj = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());

                Enum.TryParse(responseObj?.Status, false, out batchStatus);

                if (batchStatus == FssBatchStatus.Committed)
                    break;

                _logger.LogInformation(EventIds.BatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchId} completed. Batch Status is {BatchStatus} at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
            return batchStatus;
        }

        public async Task<BatchDetail> GetBatchDetails(string batchId)
        {
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";

            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4MzI4MDY1LCJuYmYiOjE2NTgzMjgwNjUsImV4cCI6MTY1ODMzMjA2MywiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQVVhYk94Y0JGLzZPeWdKdGV6byt1YnNJUnI3UlhWanQxcEQxMDhoRUdmVkpObWFMclJ0MzV1YU9oU0oyY1JCV2ExWlZDOTB4YXk2Rnp6SUFCNVpiSzIvZ01qc29ENVYzUUlqaUxEWGZBV1hoWGVDM25sTzZOeCtkWVhYS3BKelhnQzIwTFQrVFFLcDlPcUlCeTlsbjh4d09EZWc3UC81OGpWOUNDVE5OZXVKYz0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoieTh1YmppV1VFMDJuclEyZF9DUTRBQSIsInZlciI6IjEuMCJ9.HmFlNP6R62JiHXIBg6exbjGhopg3ujRkcEnle24399-aVlsWDCJ-6XA53sX8jpwVYFLg4OTa4Q8864eFvRywbZ_jnN2XAjzT2jK_BWLO2zDUrcbzgVDi1pP9P-hEox0fsbTgT-cnOX7Km7iiO4nRlMhcfwulOmnVnfeezbDJlUU9_ni_rxQRXIwyCVxr2TNf4aYFEfDiPm36iPk66jmGasjZXVCA2xFw7qU9GgHWX8Hos4AblFFmcys0El50q6PiI3e2uXSw-4Tb5aU_eadjDL5aKkcuDnj9aOgmh_YzamZCWaMzO7cg43H_Ni2HUJcoHaWE5WeJS7DlXyBmdytG7Q";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);
            HttpResponseMessage batchDetailResponse = await _fssApiClient.GetBatchDetailsAsync(uri, accessToken);

            if (!batchDetailResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Request to get batch details for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, batchDetailResponse.StatusCode.ToString(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return null;
            }
            return JsonConvert.DeserializeObject<BatchDetail>(await batchDetailResponse.Content.ReadAsStringAsync());
        }

        public async Task<string> DownloadFile(string downloadPath, string fileName, string fileLink)
        {
            string filePath = string.Empty;
            string fileUri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4MzI4MDY1LCJuYmYiOjE2NTgzMjgwNjUsImV4cCI6MTY1ODMzMjA2MywiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQVVhYk94Y0JGLzZPeWdKdGV6byt1YnNJUnI3UlhWanQxcEQxMDhoRUdmVkpObWFMclJ0MzV1YU9oU0oyY1JCV2ExWlZDOTB4YXk2Rnp6SUFCNVpiSzIvZ01qc29ENVYzUUlqaUxEWGZBV1hoWGVDM25sTzZOeCtkWVhYS3BKelhnQzIwTFQrVFFLcDlPcUlCeTlsbjh4d09EZWc3UC81OGpWOUNDVE5OZXVKYz0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoieTh1YmppV1VFMDJuclEyZF9DUTRBQSIsInZlciI6IjEuMCJ9.HmFlNP6R62JiHXIBg6exbjGhopg3ujRkcEnle24399-aVlsWDCJ-6XA53sX8jpwVYFLg4OTa4Q8864eFvRywbZ_jnN2XAjzT2jK_BWLO2zDUrcbzgVDi1pP9P-hEox0fsbTgT-cnOX7Km7iiO4nRlMhcfwulOmnVnfeezbDJlUU9_ni_rxQRXIwyCVxr2TNf4aYFEfDiPm36iPk66jmGasjZXVCA2xFw7qU9GgHWX8Hos4AblFFmcys0El50q6PiI3e2uXSw-4Tb5aU_eadjDL5aKkcuDnj9aOgmh_YzamZCWaMzO7cg43H_Ni2HUJcoHaWE5WeJS7DlXyBmdytG7Q";
            //string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(fileUri, accessToken);

            if (!fileDownloadResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Downloading of file failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", fileDownloadResponse.StatusCode.ToString(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return null;
            }

            await CopyFileToFolder(fileDownloadResponse, Path.Combine(downloadPath, fileName));

            return downloadPath;
        }

        private async Task CopyFileToFolder(HttpResponseMessage httpResponse, string downloadPath)
        {
            try
            {
                var stream = await httpResponse.Content.ReadAsStreamAsync();
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                byte[] bytes = ms.ToArray();

                MemoryStream memoryStream = new(bytes);

                if (memoryStream != null)
                {
                    using (var outputFileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        memoryStream.CopyTo(outputFileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}
