using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using UKHO.BESS.BuilderService.Services;
using UKHO.PeriodicOutputService.Common.Extensions;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.BuilderService
{
    [ExcludeFromCodeCoverage]
    public class BessBuilderServiceJob
    {
        private readonly ILogger<BessBuilderServiceJob> logger;
        private readonly IBuilderService builderService;

        public BessBuilderServiceJob(ILogger<BessBuilderServiceJob> logger, IBuilderService builderService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.builderService = builderService ?? throw new ArgumentNullException(nameof(builderService));
        }

        public async Task ProcessQueueMessage([QueueTrigger("%BessStorageConfiguration:QueueName%")] QueueMessage message)
        {
            try
            {
                ConfigQueueMessage configQueueMessage = message.Body.ToObjectFromJson<ConfigQueueMessage>();
                CommonHelper.CorrelationID = Guid.Parse(configQueueMessage.CorrelationId);

                logger.LogInformation(EventIds.BessBuilderServiceStarted.ToEventId(),
                    "Bess Builder Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateBespokeExchangeSetRequestStart,
                    EventIds.CreateBespokeExchangeSetRequestCompleted,
                    "Create Bespoke Exchange Set for Config Name:{Name} and _X-Correlation-ID:{CorrelationId}",
                    async () => await builderService.CreateBespokeExchangeSetAsync(configQueueMessage),
                    configQueueMessage.Name, CommonHelper.CorrelationID);

                logger.LogInformation(EventIds.BessBuilderServiceCompleted.ToEventId(),
                    "Bess Builder Service Completed | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occurred while processing Bess Builder Service webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
