using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Models;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public class FssBatchService : IFssBatchService
    {
        private readonly IOptions<FssApiConfiguration> _fssApiConfiguration;
        private readonly ILogger<FssBatchService> _logger;
        private readonly IFssApiClient _fssApiClient;
        private readonly IAuthFssTokenProvider _authFssTokenProvider;

        public FssBatchService(ILogger<FssBatchService> logger,
                               IOptions<FssApiConfiguration> fssApiConfiguration,
                               IFssApiClient fssApiClient,
                               IAuthFssTokenProvider authFssTokenProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fssApiConfiguration = fssApiConfiguration ?? throw new ArgumentNullException(nameof(fssApiConfiguration));
            _fssApiClient = fssApiClient ?? throw new ArgumentNullException(nameof(fssApiClient));
            _authFssTokenProvider = authFssTokenProvider ?? throw new ArgumentNullException(nameof(authFssTokenProvider));
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string url)
        {
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string batchId = CommonHelper.ExtractBatchId(url);
            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}status";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

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

                _logger.LogInformation(EventIds.BatchStatusRequestCompleted.ToEventId(), "Request to get batch status for BatchID - {BatchId} completed. Batch Status is {BatchStatus} at {DateTime} | _X-Correlation-ID:{CorrelationId}", batchId, batchStatus, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
            return batchStatus;
        }
    }
}
