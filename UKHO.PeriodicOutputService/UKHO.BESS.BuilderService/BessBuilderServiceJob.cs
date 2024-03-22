using Microsoft.Extensions.Logging;
using UKHO.BESS.BuilderService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.BuilderService
{
    public class BessBuilderServiceJob
    {
        private readonly ILogger<BessBuilderServiceJob> logger;
        private readonly IBuilderService builderService;

        public BessBuilderServiceJob(ILogger<BessBuilderServiceJob> logger, IBuilderService builderService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.builderService = builderService ?? throw new ArgumentNullException(nameof(builderService));
        }

        public async Task CreateBespokeExchangeSetAsync()
        {
            logger.LogInformation(EventIds.BessBuilderServiceStarted.ToEventId(),
                "Bess Builder Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            await Task.Run(async () => await builderService.CreateBespokeExchangeSet());

            logger.LogInformation(EventIds.BessBuilderServiceCompleted.ToEventId(),
                "Bess Builder Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
