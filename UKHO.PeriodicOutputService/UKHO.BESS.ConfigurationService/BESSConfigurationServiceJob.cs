using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService
{
    public class BESSConfigurationServiceJob
    {
        private readonly ILogger<BESSConfigurationServiceJob> logger;
        private readonly IConfigurationService configurationService;

        public BESSConfigurationServiceJob(ILogger<BESSConfigurationServiceJob> logger, IConfigurationService configurationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configurationService = configurationService;
        }

        public void Start()
        {
            logger.LogInformation(EventIds.BESSConfigurationServiceStarted.ToEventId(),
                "BESS Configuration Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

            List<BessConfig> configDetails = configurationService.ProcessConfigs();

            if (configDetails.Any())
            {
                configurationService.ScheduleConfigDetails(configDetails);
            }

            logger.LogInformation(EventIds.BESSConfigurationServiceCompleted.ToEventId(),
                "BESS Configuration Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
        }
    }
}
