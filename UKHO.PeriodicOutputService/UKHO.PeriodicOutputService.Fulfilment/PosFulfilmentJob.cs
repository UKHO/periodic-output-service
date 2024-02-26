using System.Diagnostics.CodeAnalysis;
using Elastic.Apm.Api;
using Elastic.Apm;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
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
            await Elastic.Apm.Agent.Tracer
                .CaptureTransaction("POSTransaction", ApiConstants.TypeRequest, async () =>
                {
                    //application code that is captured as a transaction
                    await CreatePosExchangeSets();
                });
        }

        private async Task CreatePosExchangeSets()
        {
            var transaction = Agent.Tracer.CurrentTransaction;
            ISpan span = transaction.StartSpan("POSJobStarted", ApiConstants.TypeApp, ApiConstants.SubTypeInternal);

            try
            {
                _logger.LogInformation(EventIds.PosFulfilmentJobStarted.ToEventId(),
                    "Periodic Output Service webjob started at {DateTime} | _X-Correlation-ID : {CorrelationId}",
                    DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                bool result = await _fulfilmentDataService.CreatePosExchangeSets();

                _logger.LogInformation(EventIds.PosFulfilmentJobCompleted.ToEventId(),
                    "Periodic Output Service webjob completed at {DateTime} | _X-Correlation-ID : {CorrelationId}",
                    DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(EventIds.UnhandledException.ToEventId(),
                    "Exception occured while processing Periodic Output Service webjob at {DateTime} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}",
                    DateTime.Now.ToUniversalTime(), ex.Message, ex.StackTrace, CommonHelper.CorrelationID);

                transaction.CaptureException(ex);

                throw;
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

                transaction.End();
            }
        }
    }
}
