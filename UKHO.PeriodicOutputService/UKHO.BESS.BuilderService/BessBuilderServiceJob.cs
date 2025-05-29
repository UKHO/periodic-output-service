using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using UKHO.BESS.BuilderService.Services;
using UKHO.PeriodicOutputService.Common.Extensions;
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
            ConfigQueueMessage configQueueMessage = message.Body.ToObjectFromJson<ConfigQueueMessage>();
            try
            {
                await Elastic.Apm.Agent.Tracer
                    .CaptureTransaction("BessBuilderTransaction", ApiConstants.TypeRequest, async () =>
                    {
                        //application code that is captured as a transaction
                        await logger.LogStartEndAndElapsedTimeAsync(EventIds.BessBuilderServiceStarted,
                            EventIds.BessBuilderServiceCompleted,
                            "Create Bespoke Exchange Set Started for Config Name:{Name} and _X-Correlation-ID:{CorrelationId}",
                            "Create Bespoke Exchange Set Completed for Config Name:{Name} and _X-Correlation-ID:{CorrelationId}",
                            async () => await builderService.CreateBespokeExchangeSetAsync(configQueueMessage),
                            configQueueMessage.Name, configQueueMessage.CorrelationId);

                    });
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.UnhandledException.ToEventId(),
                    "Exception occurred while processing Bess Builder Service webjob with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}",
                    ex.Message, ex.StackTrace, configQueueMessage.CorrelationId);

                Agent.Tracer.CurrentTransaction?.CaptureException(ex);
            }
            finally
            {
                Agent.Tracer.CurrentTransaction?.End();
            }
        }
    }
}
