using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.ConfigurationService
{
    public class BESSConfigurationServiceJob
    {
        private readonly ILogger<BESSConfigurationServiceJob> logger;
        private readonly IConfigurationFileReaderService configurationFileReaderService;

        public BESSConfigurationServiceJob(ILogger<BESSConfigurationServiceJob> logger, IConfigurationFileReaderService configurationFileReaderService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configurationFileReaderService = configurationFileReaderService;
        }

        public async Task CreateBespokeExchangeSetAsync()
        {
            try
            {
                logger.LogInformation(EventIds.BESSConfigurationServiceStarted.ToEventId(), "BESS Configuration Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                await configurationFileReaderService.ReadConfigurationJsonFiles();

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
