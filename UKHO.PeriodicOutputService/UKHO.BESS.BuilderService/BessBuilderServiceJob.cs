using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService
{
    [ExcludeFromCodeCoverage]
    public class BessBuilderServiceJob
    {
        private readonly ILogger<BessBuilderServiceJob> logger;
        private readonly IEssService essService;

        public BessBuilderServiceJob(ILogger<BessBuilderServiceJob> logger, IEssService essService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
        }

        public async Task ProcessQueueMessage([QueueTrigger("%BessStorageConfiguration:QueueName%")] QueueMessage message)
        {
            try
            {
                ConfigQueueMessage configQueueMessage = message.Body.ToObjectFromJson<ConfigQueueMessage>();

                logger.LogInformation(EventIds.BessBuilderServiceStarted.ToEventId(),
                    "Bess Builder Service Started | _X-Correlation-ID : {CorrelationId}", configQueueMessage.CorrelationId);

                await essService.PostProductIdentifiersData(configQueueMessage.EncCellNames.ToList(), configQueueMessage.ExchangeSetStandard);

                logger.LogInformation(EventIds.BessBuilderServiceCompleted.ToEventId(),
                    "Bess Builder Service Completed | _X-Correlation-ID : {CorrelationId}", configQueueMessage.CorrelationId);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.UnhandledException.ToEventId(), "Exception occurred while processing Bess Builder Service webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}", ex.Message, ex.StackTrace, CommonHelper.CorrelationID);
                throw;
            }
        }
    }
}
