using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Extensions;
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

        public async Task StartAsync()
        {
            try
            {
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.BessConfigurationServiceStarted,
                            EventIds.BessConfigurationServiceCompleted,
                            "BESS Configuration Service Started | _X-Correlation-ID : {CorrelationId}",
                            "BESS Configuration Service Completed | _X-Correlation-ID : {CorrelationId}",
                            async () => await configurationService.ProcessConfigsAsync(),
                             CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occurred while processing BESS Configuration Service webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
