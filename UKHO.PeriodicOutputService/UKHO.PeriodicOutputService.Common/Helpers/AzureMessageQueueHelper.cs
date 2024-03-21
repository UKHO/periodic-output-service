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
    public async Task AddMessage(string message)
    {
        // Create the queue client.
        var queueClient = new QueueClient(bessStorageConfiguration.Value.ConnectionString, bessStorageConfiguration.Value.QueueName);

        // Create the queue if it doesn't already exist.
        await queueClient.CreateIfNotExistsAsync();

        // convert message to base64string
        var messageBase64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
        // Send a message to the queue
        await queueClient.SendMessageAsync(messageBase64String);

        logger.LogInformation(EventIds.BessConfigPropertiesAddedInQueue.ToEventId(), "Added message in Queue:{queue}, QueueMessage: {QueueMessage} and _X-Correlation-ID:{CorrelationId}", bessStorageConfiguration.Value.QueueName, message, CommonHelper.CorrelationID);
    }
}

