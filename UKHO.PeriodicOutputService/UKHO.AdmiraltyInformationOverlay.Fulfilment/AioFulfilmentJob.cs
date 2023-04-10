using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.AdmiraltyInformationOverlay.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public class AioFulfilmentJob
    {
        private readonly ILogger<AioFulfilmentJob> _logger;

        public AioFulfilmentJob(ILogger<AioFulfilmentJob> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ProcessFulfilmentJob()
        {
            try
            {
                _logger.LogInformation(EventIds.AIOFulfilmentJobStarted.ToEventId(), "AIO webjob started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                _logger.LogInformation(EventIds.AIOFulfilmentJobEnded.ToEventId(), "AIO  webjob ended | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing AIOFulfilment webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
            }
        }
    }
}
