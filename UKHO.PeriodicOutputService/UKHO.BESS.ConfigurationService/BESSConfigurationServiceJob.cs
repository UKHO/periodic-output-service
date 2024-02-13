using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.ConfigurationService
{
    public class BESSConfigurationServiceJob
    {
        private readonly ILogger<BESSConfigurationServiceJob> logger;
        public BESSConfigurationServiceJob(ILogger<BESSConfigurationServiceJob> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CreateBespokeExchangeSetAsync()
        {
            logger.LogInformation(EventIds.BESSConfigurationServiceStarted.ToEventId(),
                "BESS Configuration Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            await Task.CompletedTask; // temporary code

            logger.LogInformation(EventIds.BESSConfigurationServiceCompleted.ToEventId(),
                "BESS Configuration Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);           
        }
    }
}
