using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureMessageQueueHelper : IAzureMessageQueueHelper
    {
        private readonly ILogger<AzureMessageQueueHelper> logger;
        private readonly BessStorageConfiguration bessStorageConfiguration;
        public AzureMessageQueueHelper(ILogger<AzureMessageQueueHelper> logger, IOptions<BessStorageConfiguration> bessStorageConfiguration)
        {
            this.logger = logger;
            this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
        }
        public async Task AddMessage(string message)
        {
            // Create the queue client.
            var queueClient = new QueueClient(bessStorageConfiguration.ConnectionString, bessStorageConfiguration.QueueName);

            // Create the queue if it doesn't already exist.
            await queueClient.CreateIfNotExistsAsync();

            // convert message to base64string
            var messageBase64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            // Send a message to the queue
            await queueClient.SendMessageAsync(messageBase64String);
        }
    }
}
