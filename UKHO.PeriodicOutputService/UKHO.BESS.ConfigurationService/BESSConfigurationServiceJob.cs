using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

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
            try
            {
                logger.LogInformation(EventIds.BESSConfigurationServiceStarted.ToEventId(), "BESS Configuration Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                //await configurationService.ReadConfigurationJsonFiles();
                configurationService.ProcessConfigs();

                logger.LogInformation(EventIds.BESSConfigurationServiceCompleted.ToEventId(), "BESS Configuration Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing Configuration Service webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
