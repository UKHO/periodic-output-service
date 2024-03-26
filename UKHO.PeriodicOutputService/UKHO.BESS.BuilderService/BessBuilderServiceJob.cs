using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.BuilderService
{
    [ExcludeFromCodeCoverage]
    public class BessBuilderServiceJob
    {
        private readonly ILogger<BessBuilderServiceJob> logger;

        public BessBuilderServiceJob(ILogger<BessBuilderServiceJob> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessQueueMessage([QueueTrigger("%BessStorageConfiguration:QueueName%")] QueueMessage message)
        {
            try
            {
                logger.LogInformation(EventIds.BessBuilderServiceStarted.ToEventId(),
                    "Bess Builder Service Started | _X-Correlation-ID : {CorrelationId}", CommonHelper.CorrelationID);

                ConfigQueueMessage configQueueMessage = message.Body.ToObjectFromJson<ConfigQueueMessage>();

                await Task.CompletedTask; // temporary code

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
