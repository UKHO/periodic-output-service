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

        public async Task<string> CreateBatch()
        {
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NTAwODMyLCJuYmYiOjE2NTg1MDA4MzIsImV4cCI6MTY1ODUwNjMxMSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXIrYktXd1M4TG9OL2JTK3hmZ0FPNGVjUmRqT0R3NHhEalQxcThDTmFSK1dZdHZiT3VQcm5uckhicWF6MmZCcHZTbEZMNWljWEx0N3EyTUNpYUM4eE4rSERSalZRTnJLWU9wbkIzTHhvaDdybEtMRXVhaG4wZUdkQzF6bFhvdnE1djg5MWkrTGpWL2NIZ2dxNEdyVVNZNmJEL3o0Um1zbGNvd2J6alhBaEFMTT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiRzdTbGJxWXA2a3VHeXRmZjZnbGRBQSIsInZlciI6IjEuMCJ9.a-YKAAw7ip_k_eS8ELAXM6U64KHXptUzzGye5GtI5smdopqxrUFU8xkPbrxUrvv0NlI_J42LMlbyCbuZ69yenWmPhy2aVzTumZTzTJrwlpqYhtc8IIHvtq2vEBHPJBW_MqbpcItfY6G6ow6T6E1QCPtZlYPpkS70yM0I4zoEci-dXRMUXow73l9pHcS7oIt4uTD5dX0UEtIkWo4OfZNYWqQgV1GPrgJbOOdcLHvYJRH1W6l-Q5nQnmAbt84OGcpT48yJdTFmb11bGctISjpA7sOjqxjRjLMJgFVNpjoNtsC89GN_dbHrQRKz2A6dSkNucD68rRYG1VSmXT4DAZ7bTA";
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
            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NTAwODMyLCJuYmYiOjE2NTg1MDA4MzIsImV4cCI6MTY1ODUwNjMxMSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXIrYktXd1M4TG9OL2JTK3hmZ0FPNGVjUmRqT0R3NHhEalQxcThDTmFSK1dZdHZiT3VQcm5uckhicWF6MmZCcHZTbEZMNWljWEx0N3EyTUNpYUM4eE4rSERSalZRTnJLWU9wbkIzTHhvaDdybEtMRXVhaG4wZUdkQzF6bFhvdnE1djg5MWkrTGpWL2NIZ2dxNEdyVVNZNmJEL3o0Um1zbGNvd2J6alhBaEFMTT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiRzdTbGJxWXA2a3VHeXRmZjZnbGRBQSIsInZlciI6IjEuMCJ9.a-YKAAw7ip_k_eS8ELAXM6U64KHXptUzzGye5GtI5smdopqxrUFU8xkPbrxUrvv0NlI_J42LMlbyCbuZ69yenWmPhy2aVzTumZTzTJrwlpqYhtc8IIHvtq2vEBHPJBW_MqbpcItfY6G6ow6T6E1QCPtZlYPpkS70yM0I4zoEci-dXRMUXow73l9pHcS7oIt4uTD5dX0UEtIkWo4OfZNYWqQgV1GPrgJbOOdcLHvYJRH1W6l-Q5nQnmAbt84OGcpT48yJdTFmb11bGctISjpA7sOjqxjRjLMJgFVNpjoNtsC89GN_dbHrQRKz2A6dSkNucD68rRYG1VSmXT4DAZ7bTA";
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

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string batchId)
        {
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{_fssApiConfiguration.Value.BaseUrl}/batch/{batchId}/status";

            _logger.LogInformation(EventIds.GetFssAuthTokenRequestStarted.ToEventId(), "Getting access token to call FSS Batch Status endpoint started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NTAwODMyLCJuYmYiOjE2NTg1MDA4MzIsImV4cCI6MTY1ODUwNjMxMSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXIrYktXd1M4TG9OL2JTK3hmZ0FPNGVjUmRqT0R3NHhEalQxcThDTmFSK1dZdHZiT3VQcm5uckhicWF6MmZCcHZTbEZMNWljWEx0N3EyTUNpYUM4eE4rSERSalZRTnJLWU9wbkIzTHhvaDdybEtMRXVhaG4wZUdkQzF6bFhvdnE1djg5MWkrTGpWL2NIZ2dxNEdyVVNZNmJEL3o0Um1zbGNvd2J6alhBaEFMTT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiRzdTbGJxWXA2a3VHeXRmZjZnbGRBQSIsInZlciI6IjEuMCJ9.a-YKAAw7ip_k_eS8ELAXM6U64KHXptUzzGye5GtI5smdopqxrUFU8xkPbrxUrvv0NlI_J42LMlbyCbuZ69yenWmPhy2aVzTumZTzTJrwlpqYhtc8IIHvtq2vEBHPJBW_MqbpcItfY6G6ow6T6E1QCPtZlYPpkS70yM0I4zoEci-dXRMUXow73l9pHcS7oIt4uTD5dX0UEtIkWo4OfZNYWqQgV1GPrgJbOOdcLHvYJRH1W6l-Q5nQnmAbt84OGcpT48yJdTFmb11bGctISjpA7sOjqxjRjLMJgFVNpjoNtsC89GN_dbHrQRKz2A6dSkNucD68rRYG1VSmXT4DAZ7bTA";
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

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NTAwODMyLCJuYmYiOjE2NTg1MDA4MzIsImV4cCI6MTY1ODUwNjMxMSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXIrYktXd1M4TG9OL2JTK3hmZ0FPNGVjUmRqT0R3NHhEalQxcThDTmFSK1dZdHZiT3VQcm5uckhicWF6MmZCcHZTbEZMNWljWEx0N3EyTUNpYUM4eE4rSERSalZRTnJLWU9wbkIzTHhvaDdybEtMRXVhaG4wZUdkQzF6bFhvdnE1djg5MWkrTGpWL2NIZ2dxNEdyVVNZNmJEL3o0Um1zbGNvd2J6alhBaEFMTT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiRzdTbGJxWXA2a3VHeXRmZjZnbGRBQSIsInZlciI6IjEuMCJ9.a-YKAAw7ip_k_eS8ELAXM6U64KHXptUzzGye5GtI5smdopqxrUFU8xkPbrxUrvv0NlI_J42LMlbyCbuZ69yenWmPhy2aVzTumZTzTJrwlpqYhtc8IIHvtq2vEBHPJBW_MqbpcItfY6G6ow6T6E1QCPtZlYPpkS70yM0I4zoEci-dXRMUXow73l9pHcS7oIt4uTD5dX0UEtIkWo4OfZNYWqQgV1GPrgJbOOdcLHvYJRH1W6l-Q5nQnmAbt84OGcpT48yJdTFmb11bGctISjpA7sOjqxjRjLMJgFVNpjoNtsC89GN_dbHrQRKz2A6dSkNucD68rRYG1VSmXT4DAZ7bTA";
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

            string accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU4NTAwODMyLCJuYmYiOjE2NTg1MDA4MzIsImV4cCI6MTY1ODUwNjMxMSwiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQXIrYktXd1M4TG9OL2JTK3hmZ0FPNGVjUmRqT0R3NHhEalQxcThDTmFSK1dZdHZiT3VQcm5uckhicWF6MmZCcHZTbEZMNWljWEx0N3EyTUNpYUM4eE4rSERSalZRTnJLWU9wbkIzTHhvaDdybEtMRXVhaG4wZUdkQzF6bFhvdnE1djg5MWkrTGpWL2NIZ2dxNEdyVVNZNmJEL3o0Um1zbGNvd2J6alhBaEFMTT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNzMuMjQ0LjExMSIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBpVGdXNEFJb3Z0QXEyODVuQ1pIMHpRQ0FCOC4iLCJyb2xlcyI6WyJCYXRjaENyZWF0ZSJdLCJzY3AiOiJVc2VyLlJlYWQiLCJzdWIiOiI2VEhEZDlIaXU5bER2R0U2UTl6OUhOc2lUR1hBRnRuS05xNnpnVUlLNDVRIiwidGlkIjoiOTEzNGNhNDgtNjYzZC00YTA1LTk2OGEtMzFhNDJmMGFlZDNlIiwidW5pcXVlX25hbWUiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwidXRpIjoiRzdTbGJxWXA2a3VHeXRmZjZnbGRBQSIsInZlciI6IjEuMCJ9.a-YKAAw7ip_k_eS8ELAXM6U64KHXptUzzGye5GtI5smdopqxrUFU8xkPbrxUrvv0NlI_J42LMlbyCbuZ69yenWmPhy2aVzTumZTzTJrwlpqYhtc8IIHvtq2vEBHPJBW_MqbpcItfY6G6ow6T6E1QCPtZlYPpkS70yM0I4zoEci-dXRMUXow73l9pHcS7oIt4uTD5dX0UEtIkWo4OfZNYWqQgV1GPrgJbOOdcLHvYJRH1W6l-Q5nQnmAbt84OGcpT48yJdTFmb11bGctISjpA7sOjqxjRjLMJgFVNpjoNtsC89GN_dbHrQRKz2A6dSkNucD68rRYG1VSmXT4DAZ7bTA";
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
                BusinessUnit = "AVCSCustomExchangeSets",
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
    }
}
