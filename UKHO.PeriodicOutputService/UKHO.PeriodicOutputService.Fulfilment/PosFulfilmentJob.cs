using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Logging;
using System.Diagnostics.CodeAnalysis;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public class PosFulfilmentJob
    {
        private readonly IFulfilmentDataService _fulfilmentDataService;
        private readonly ILogger<PosFulfilmentJob> _logger;

        public PosFulfilmentJob(ILogger<PosFulfilmentJob> logger, IFulfilmentDataService fulfilmentDataService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fulfilmentDataService = fulfilmentDataService ?? throw new ArgumentNullException(nameof(fulfilmentDataService));
        }

        public async Task ProcessFulfilmentJob()
        {
            Guid tempCorrelationId = Guid.NewGuid();
            try
            {
                _logger.LogInformation(EventIds.POSRequestStarted.ToEventId(), "Periodic Output Service Web job started at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), tempCorrelationId);

                string result = await _fulfilmentDataService.CreatePosExchangeSet();

                _logger.LogInformation(EventIds.POSRequestCompleted.ToEventId(), "Periodic Output Service Web job completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), tempCorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing Periodic Output Service web job set at {DateTime} | Exception:{Message} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), ex.Message, tempCorrelationId);
            }
        }
    }
}
