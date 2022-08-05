using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
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
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestCompleted.ToEventId(), "Getting access token to call FSS Batch Status endpoint completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(_fssApiConfiguration.Value.BatchStatusPollingCutoffTime)))
            {
                _logger.LogInformation(EventIds.BatchStatusRequestStarted.ToEventId(), "Request to get batch status for BatchID - {BatchId} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                HttpResponseMessage? batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(uri, accessToken);

                if (!batchStatusResponse.IsSuccessStatusCode)
                {
                    _logger.LogError(EventIds.BatchStatusRequestFailed.ToEventId(), "Request to get batch status for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchStatusResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                    throw new FulfilmentException(EventIds.BatchStatusRequestFailed.ToEventId());
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
            _logger.LogInformation(EventIds.GetBatchDetailRequestStarted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS started | {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}";

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage batchDetailResponse = await _fssApiClient.GetBatchDetailsAsync(uri, accessToken);

            if (batchDetailResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation(EventIds.GetBatchDetailRequestCompleted.ToEventId(), "Request to get batch details for BatchID - {BatchId} from FSS completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return JsonConvert.DeserializeObject<GetBatchResponseModel>(await batchDetailResponse.Content.ReadAsStringAsync());
            }
            else
            {
                _logger.LogError(EventIds.GetBatchDetailRequestFailed.ToEventId(), "Request to get batch details for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", batchId, DateTime.Now.ToUniversalTime(), batchDetailResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.GetBatchDetailRequestFailed.ToEventId());
            }
        }

        public async Task<Stream> DownloadFile(string downloadPath, string fileName, string fileLink)
        {
            _logger.LogInformation(EventIds.DownloadFileStarted.ToEventId(), "Downloading file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string fileUri = $"{_fssApiConfiguration.Value.BaseUrl}" + fileLink;

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            HttpResponseMessage fileDownloadResponse = await _fssApiClient.DownloadFile(fileUri, accessToken);

            if (!fileDownloadResponse.IsSuccessStatusCode)
            {
                _logger.LogError(EventIds.DownloadFileFailed.ToEventId(), "Downloading file {fileName} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), fileDownloadResponse.StatusCode.ToString(), CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.DownloadFileFailed.ToEventId());
            }

            _logger.LogInformation(EventIds.DownloadFileCompleted.ToEventId(), "Downloading file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", fileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return await fileDownloadResponse.Content.ReadAsStreamAsync();
        }
    }
}
