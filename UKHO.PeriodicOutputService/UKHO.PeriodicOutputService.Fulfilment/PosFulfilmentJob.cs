using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Logging;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    public class PosFulfilmentJob
    {
        private readonly ILogger<PosFulfilmentJob> _logger;
        public PosFulfilmentJob(ILogger<PosFulfilmentJob> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [NoAutomaticTrigger]
        public async Task ProcessWebJob()
        {
            Guid guid = Guid.NewGuid();
            try
            {
                _logger.LogInformation(EventIds.POSRequestStarted.ToEventId(), "Periodic Output Service Web job started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), guid);

                await Task.CompletedTask;

                _logger.LogInformation(EventIds.POSRequestCompleted.ToEventId(), "Periodic Output Service Web job completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing Periodic Output Service web job set at {DateTime} | Exception:{Message} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), ex.Message, guid);
            }
        }
    }
}
