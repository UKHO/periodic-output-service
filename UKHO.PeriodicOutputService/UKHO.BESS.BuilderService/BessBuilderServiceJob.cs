using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.BuilderService
{
    public class BessBuilderServiceJob
    {
        private readonly ILogger<BessBuilderServiceJob> logger;

        public BessBuilderServiceJob(ILogger<BessBuilderServiceJob> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CreateBespokeExchangeSetAsync()
        {
            logger.LogInformation(EventIds.BessBuilderServiceStarted.ToEventId(),
                "Bess Builder Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            await Task.CompletedTask; // temporary code

            logger.LogInformation(EventIds.BessBuilderServiceCompleted.ToEventId(),
                "Bess Builder Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
