using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.PeriodicOutputService.Common.Helpers;

[ExcludeFromCodeCoverage]
public class AzureMessageQueueHelper : IAzureMessageQueueHelper
{
    private readonly ILogger<AzureMessageQueueHelper> logger;
    private readonly IOptions<BessStorageConfiguration> bessStorageConfiguration;

    public AzureMessageQueueHelper(ILogger<AzureMessageQueueHelper> logger, IOptions<BessStorageConfiguration> bessStorageConfiguration)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.bessStorageConfiguration = bessStorageConfiguration ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
    }

    public async Task AddMessageAsync(string message, string configName, string fileName, string builderServiceCorrelationId)
    {
        // Create the queue client.
        var queueClient = new QueueClient(bessStorageConfiguration.Value.ConnectionString, bessStorageConfiguration.Value.QueueName);

        // Create the queue if it doesn't already exist.
        this.logger.LogInformation(EventIds.BessQueueCreated.ToEventId(), $"Creating queue now: connectionstring:{bessStorageConfiguration.Value.ConnectionString},queue name:{bessStorageConfiguration.Value.QueueName}}");

        var queueResult = await queueClient.CreateIfNotExistsAsync();

        if (queueResult != null)
        {
            this.logger.LogInformation(EventIds.BessQueueCreated.ToEventId(), "Queue created:{queue}, BuilderService_X-Correlation-ID:{builderServiceCorrelationId} and _X-Correlation-ID:{CorrelationId}", bessStorageConfiguration.Value.QueueName, builderServiceCorrelationId, CommonHelper.CorrelationID);
        }

        // convert message to base64string
        var messageBase64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
        // Send a message to the queue
        await queueClient.SendMessageAsync(messageBase64String);

        logger.LogInformation(EventIds.BessConfigPropertiesAddedInQueue.ToEventId(), "Added message in Queue:{queue}, Filename:{filename}, Configname:{configname}, BuilderService_X-Correlation-ID:{builderServiceCorrelationId} and _X-Correlation-ID:{CorrelationId}", bessStorageConfiguration.Value.QueueName, fileName, configName, builderServiceCorrelationId, CommonHelper.CorrelationID);
    }
}
