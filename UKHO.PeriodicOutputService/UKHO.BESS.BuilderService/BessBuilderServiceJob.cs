using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.BuilderService
{
    public class BessBuilderServiceJob
    {
        private readonly ILogger<BessBuilderServiceJob> _logger;
        public BessBuilderServiceJob(ILogger<BessBuilderServiceJob> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CreateBespokeExchangeSetAsync()
        {
            _logger.LogInformation(EventIds.BessBuilderServiceStarted.ToEventId(),
                "Bess Builder Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            await Task.CompletedTask; // temporary code

            _logger.LogInformation(EventIds.BessBuilderServiceCompleted.ToEventId(),
                "Bess Builder Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
