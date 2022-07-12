using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
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

        public async Task<string> CheckIfBatchCommitted(string url)
        {
            string batchStatus = "";
            var startTime = DateTime.UtcNow;

            ////_logger.LogInformation(EventIds.AccessTokenForESSEndpointStarted.ToEventId(), "Getting access token to call ESS endpoint started");

            string accessToken = await _authFssTokenProvider.GetManagedIdentityAuthAsync(_fssApiConfiguration.Value.FssClientId);

            ////_logger.LogInformation(EventIds.AccessTokenForESSEndpointCompleted.ToEventId(), "Getting access token  to call ESS endpoint completed");

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(90))
            {
                await Task.Delay(1000);

                Console.WriteLine("Polling fss to get batch status...");

                var batchStatusResponse = await _fssApiClient.GetBatchStatusAsync(url, accessToken);

                FssBatchStatusResponseModel responseObj = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                batchStatus = responseObj.Status;

                Console.WriteLine("Batch status is - {0}", batchStatus);

                if (string.IsNullOrEmpty(batchStatus) || batchStatus.Equals("Committed"))
                    break;
            }
            return batchStatus;
        }
    }
}
