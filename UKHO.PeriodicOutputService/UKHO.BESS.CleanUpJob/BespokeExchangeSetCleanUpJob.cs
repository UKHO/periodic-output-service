using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob
{
    public class BespokeExchangeSetCleanUpJob
    {
        private readonly ILogger<BespokeExchangeSetCleanUpJob> logger;

        public BespokeExchangeSetCleanUpJob(ILogger<BespokeExchangeSetCleanUpJob> logger)
        {
            this.logger = logger;
        }
        public async Task ProcessCleanUp()
        {
            logger.LogInformation(EventIds.BESSCleanUpJobRequestStart.ToEventId(), "Bespoke Exchange set service clean up web job started at " + DateTime.Now);

            await Task.CompletedTask;

            logger.LogInformation(EventIds.BESSCleanUpJobRequestCompleted.ToEventId(), "Bespoke Exchange set service clean up web job completed at " + DateTime.Now);
        }
    }
}
