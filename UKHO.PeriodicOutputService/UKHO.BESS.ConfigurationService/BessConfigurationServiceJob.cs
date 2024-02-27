using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.ConfigurationService
{
    [ExcludeFromCodeCoverage]
    public class BessConfigurationServiceJob
    {
        private readonly ILogger<BessConfigurationServiceJob> logger;
        private readonly IConfigurationService configurationService;

        public BessConfigurationServiceJob(ILogger<BessConfigurationServiceJob> logger, IConfigurationService configurationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public void Start()
        {
            try
            {
                logger.LogInformation(EventIds.BessConfigurationServiceStarted.ToEventId(), "Bess Configuration Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                configurationService.ProcessConfigs();

                logger.LogInformation(EventIds.BessConfigurationServiceCompleted.ToEventId(), "Bess Configuration Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing Bess Configuration Service webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
