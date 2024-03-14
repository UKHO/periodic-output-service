using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly ILogger<AzureBlobStorageService> logger;
        private readonly IAzureMessageQueueHelper azureMessageQueueHelper;

        public AzureBlobStorageService(ILogger<AzureBlobStorageService> logger, IAzureMessageQueueHelper azureMessageQueueHelper)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.azureMessageQueueHelper = azureMessageQueueHelper ?? throw new ArgumentNullException(nameof(azureMessageQueueHelper));
        }
        public async Task<bool> SetConfigQueueMessageModelAndAddToQueue(BessConfig bessConfig, IEnumerable<string> encCellNames, int? fileSize)
        {
            if (bessConfig == null)
            {
                return false;
            }

            ConfigQueueMessage configQueueMessage = new()
            {
                Name = bessConfig.Name,
                ExchangeSetStandard = bessConfig.ExchangeSetStandard,
                EncCellNames = encCellNames,
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
                CorrelationId = CommonHelper.CorrelationID.ToString()
            };
            string configQueueMessageJSON = JsonConvert.SerializeObject(configQueueMessage);
            await azureMessageQueueHelper.AddMessage(configQueueMessageJSON);

            return true;
        }

    }
}
