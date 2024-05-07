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
        private readonly ICleanUpService cleanUpService;

        public BessCleanUpJob(ILogger<BessCleanUpJob> logger, ICleanUpService cleanUpService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.cleanUpService = cleanUpService ?? throw new ArgumentNullException(nameof(cleanUpService));
        }

        public void ProcessCleanUp()
        {
            logger.LogInformation(EventIds.BESSCleanUpJobRequestStarted.ToEventId(), "Bespoke Exchange set service clean up web job started at " + DateTime.Now + "| _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            cleanUpService.CleanUpHistoricFoldersAndFiles();

            logger.LogInformation(EventIds.BESSCleanUpJobRequestCompleted.ToEventId(), "Bespoke Exchange set service clean up web job completed at " + DateTime.Now + "| _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
