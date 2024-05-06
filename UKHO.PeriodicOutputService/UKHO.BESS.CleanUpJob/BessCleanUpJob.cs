using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.BESS.CleanUpJob.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob
{
    [ExcludeFromCodeCoverage]
    public class BessCleanUpJob
    {
        private readonly ILogger<BessCleanUpJob> logger;
        private readonly ICleanUpService bessCleanUpService;

        public BessCleanUpJob(ILogger<BessCleanUpJob> logger, ICleanUpService bessCleanUpService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.bessCleanUpService = bessCleanUpService ?? throw new ArgumentNullException(nameof(bessCleanUpService));
        }

        public async Task ProcessCleanUp()
        {
            logger.LogInformation(EventIds.BESSCleanUpJobRequestStarted.ToEventId(), "Bespoke Exchange set service clean up web job started at " + DateTime.Now + "| _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            await bessCleanUpService.CleanUpHistoricFoldersAndFiles();

            logger.LogInformation(EventIds.BESSCleanUpJobRequestCompleted.ToEventId(), "Bespoke Exchange set service clean up web job completed at " + DateTime.Now + "| _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
