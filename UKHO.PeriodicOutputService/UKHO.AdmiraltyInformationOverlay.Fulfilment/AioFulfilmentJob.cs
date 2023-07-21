using System.Diagnostics.CodeAnalysis;
using Elastic.Apm.Api;
using Elastic.Apm;
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

        public async Task ProcessFulfilmentJobAsync()
        {
            var transaction = Agent.Tracer.CurrentTransaction;
            ISpan span = transaction.StartSpan("AIOJobStarted", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                _logger.LogInformation(EventIds.AIOFulfilmentJobStarted.ToEventId(),
                    "AIO webjob started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                bool result = await _fulfilmentDataService.CreateAioExchangeSetsAsync();

                _logger.LogInformation(EventIds.AIOFulfilmentJobCompleted.ToEventId(),
                    "AIO webjob completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(),
                    "Exception occured while processing AIOFulfilment webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}",
                    ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
            }
            finally
            {
                span.End();

                transaction.TryGetLabel("FullAvcsDvdBatchCreated", out bool isFullAvcsDvdBatchCreated);
                transaction.TryGetLabel("FullAvcsZipBatchCreated", out bool isFullAvcsZipBatchCreated);
                transaction.TryGetLabel("CatalogueFileBatchCreated", out bool isCatalogueFileBatchCreated);
                transaction.TryGetLabel("EncUpdateFileBatchCreated", out bool isEncUpdateFileBatchCreated);
                transaction.TryGetLabel("UpdateZipBatchCreated", out bool isUpdateZipBatchCreated);

                transaction.SetLabel("POSBatchesCreated",
                    isFullAvcsDvdBatchCreated &&
                    isFullAvcsZipBatchCreated &&
                    isCatalogueFileBatchCreated &&
                    isEncUpdateFileBatchCreated &&
                    isUpdateZipBatchCreated);
            }
        }
    }
}
