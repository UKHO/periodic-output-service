using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Helpers;

[ExcludeFromCodeCoverage]
public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly IAzureMessageQueueHelper azureMessageQueueHelper;
    private readonly IAzureBlobStorageClient azureBlobStorageClient;
    private readonly BessStorageConfiguration bessStorageConfiguration;
    private readonly ILogger<AzureBlobStorageService> logger;

    public AzureBlobStorageService(IAzureMessageQueueHelper azureMessageQueueHelper,
                            IAzureBlobStorageClient azureBlobStorageClient,
                            IOptions<BessStorageConfiguration> bessStorageConfiguration,
                            ILogger<AzureBlobStorageService> logger)
    {
        this.azureMessageQueueHelper = azureMessageQueueHelper ?? throw new ArgumentNullException(nameof(azureMessageQueueHelper));
        this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
        this.bessStorageConfiguration = bessStorageConfiguration.Value ?? throw new ArgumentNullException(nameof(bessStorageConfiguration));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SetConfigQueueMessageModelAndAddToQueueAsync(BessConfig bessConfig, IEnumerable<string> encCellNames, long? fileSize)
    {
        if (bessConfig == null)
        {
            return false;
        }

        logger.LogInformation(EventIds.AddConfigMessageToQueueStarted.ToEventId(),
            "BESS Config Name : {Name} | _X-Correlation-ID : {CorrelationId}", bessConfig.Name, CommonHelper.CorrelationID);

        var messageDetail = new MessageDetail { EncCellNames = encCellNames };

        var (uploaded, messageBlobUri) = await UploadMessageDetailToBlobAsync(messageDetail);

        if (uploaded && messageBlobUri != null)
        {   
            ConfigQueueMessage configQueueMessage = new()
            {
                Name = bessConfig.Name,
                ExchangeSetStandard = bessConfig.ExchangeSetStandard,
                MessageDetailUri = messageBlobUri,
                //EncCellNames = encCellNames,
                Frequency = bessConfig.Frequency,
                Type = bessConfig.Type,
                KeyFileType = bessConfig.KeyFileType,
                AllowedUsers = bessConfig.AllowedUsers,
                AllowedUserGroups = bessConfig.AllowedUserGroups,
                Tags = bessConfig.Tags,
                ReadMeSearchFilter = bessConfig.ReadMeSearchFilter,
                BatchExpiryInDays = bessConfig.BatchExpiryInDays,
                IsEnabled = bessConfig.IsEnabled,
                FileName = bessConfig.FileName,
                FileSize = fileSize,
                CorrelationId = Guid.NewGuid().ToString(),
            };

            var configQueueMessageJson = JsonConvert.SerializeObject(configQueueMessage);
            await azureMessageQueueHelper.AddMessageAsync(configQueueMessageJson, bessConfig.Name, bessConfig.FileName, configQueueMessage.CorrelationId);

            logger.LogInformation(EventIds.AddConfigMessageToQueueCompleted.ToEventId(),
                "Message : {message} added for Bess Config name : {Name} | _X-Correlation-ID : {CorrelationId}", configQueueMessageJson, bessConfig.Name, CommonHelper.CorrelationID);

        }
        else
        {
            logger.LogWarning(EventIds.AddConfigMessageToQueueFailed.ToEventId(),
                "An error occurred while adding message for BESS Config : {Name} to queue. | _X-Correlation-ID : {CorrelationId}", bessConfig.Name, CommonHelper.CorrelationID);
        }
        
        return uploaded;
    }

    private async Task<(bool uploadSuccess, string? messageBlobUri)> UploadMessageDetailToBlobAsync(MessageDetail messageDetail)
    {
        var uploadSuccess = false;
        string messageBlobUri = null;
        var messageDetailJson = JsonConvert.SerializeObject(messageDetail);
        var blobName = $"bessConfig.Name{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        logger.LogInformation(EventIds.UploadConfigMessageDetailToBlobStarted.ToEventId(),
            "Uploading started for Message : {message} to blob: {blob} | _X-Correlation-ID: {CorrelationId}",
            messageDetailJson, blobName, CommonHelper.CorrelationID);
        
        var messageBlobClient = await azureBlobStorageClient.GetBlobClientAsync(bessStorageConfiguration.MessageContainerName, blobName);
        
        using var ms = new MemoryStream();
        LoadStreamWithJson(ms, messageDetailJson);

        try
        {
            await messageBlobClient.UploadAsync(ms);
            uploadSuccess = true;
            messageBlobUri = messageBlobClient.Uri?.AbsoluteUri;

            logger.LogInformation(EventIds.UploadConfigMessageDetailToBlobCompleted.ToEventId(),
                "Uploading completed for Message : {message} to blob: {file} | _X-Correlation-ID : {CorrelationId}",
                messageDetailJson, blobName, CommonHelper.CorrelationID);
        }
        catch (Exception ex)
        {
            logger.LogError(EventIds.UploadConfigMessageDetailToBlobFailed.ToEventId(),
                "An error: {error} occurred while uploading message: {message} to blob: {file} | _X-Correlation-ID: {CorrelationId}",
                ex.Message, messageDetailJson, blobName, CommonHelper.CorrelationID);
        }

        return (uploadSuccess, messageBlobUri);
    }

    private static void LoadStreamWithJson(Stream ms, object obj)
    {
        var writer = new StreamWriter(ms);
        writer.Write(obj);
        writer.Flush();
        ms.Position = 0;
    }
}
