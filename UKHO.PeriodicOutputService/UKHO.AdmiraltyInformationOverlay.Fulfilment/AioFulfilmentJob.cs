using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.AdmiraltyInformationOverlay.Fulfilment.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.AdmiraltyInformationOverlay.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public class AioFulfilmentJob
    {
        private readonly IFulfilmentDataService _fulfilmentDataService;
        private readonly ILogger<AioFulfilmentJob> _logger;

        public AioFulfilmentJob(ILogger<AioFulfilmentJob> logger, IFulfilmentDataService fulfilmentDataService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fulfilmentDataService = fulfilmentDataService ?? throw new ArgumentNullException(nameof(fulfilmentDataService));
        }

        public async Task ProcessFulfilmentJob()
        {
            try
            {
                _logger.LogInformation(EventIds.AIOFulfilmentJobStarted.ToEventId(), "AIO webjob started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                bool result = await _fulfilmentDataService.CreateAioExchangeSets();

                _logger.LogInformation(EventIds.AIOFulfilmentJobCompleted.ToEventId(), "AIO webjob completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occured while processing AIOFulfilment webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
            }
        }
    }
}
