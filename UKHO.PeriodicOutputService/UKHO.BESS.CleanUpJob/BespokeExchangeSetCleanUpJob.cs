using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.BESS.CleanUpJob.Services;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob
{
    public class BespokeExchangeSetCleanUpJob
    {
        private readonly ILogger<BespokeExchangeSetCleanUpJob> logger;
        private readonly IBespokeExchangeSetCleanUpService bespokeExchangeSetCleanUpService;

        public BespokeExchangeSetCleanUpJob(ILogger<BespokeExchangeSetCleanUpJob> logger, IBespokeExchangeSetCleanUpService bespokeExchangeSetCleanUpService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.bespokeExchangeSetCleanUpService = bespokeExchangeSetCleanUpService ?? throw new ArgumentNullException(nameof(bespokeExchangeSetCleanUpService));
        }
        public async Task ProcessCleanUp()
        {
            logger.LogInformation(EventIds.BESSCleanUpJobRequestStarted.ToEventId(), "Bespoke Exchange set service clean up web job started at " + DateTime.Now + "| _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            await bespokeExchangeSetCleanUpService.DeleteHistoricFoldersAndFiles();

            logger.LogInformation(EventIds.BESSCleanUpJobRequestCompleted.ToEventId(), "Bespoke Exchange set service clean up web job completed at " + DateTime.Now + "| _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
